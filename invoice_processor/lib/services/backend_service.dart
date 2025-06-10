import 'dart:io';
import 'dart:async';
import 'package:flutter/foundation.dart';
import 'package:dio/dio.dart';

class BackendService {
  static BackendService? _instance;
  static BackendService get instance => _instance ??= BackendService._();
  
  BackendService._();

  Process? _backendProcess;
  bool _isStarting = false;
  bool _isRunning = false;

  bool get isRunning => _isRunning;

  Future<bool> startBackend() async {
    if (_isRunning || _isStarting) {
      return _isRunning;
    }

    _isStarting = true;

    try {
      // Check if backend is already running
      if (await _isBackendAlreadyRunning()) {
        _isRunning = true;
        _isStarting = false;
        debugPrint('Backend is already running');
        return true;
      }

      // Find the backend executable
      String backendPath = await _findBackendExecutable();
      if (backendPath.isEmpty) {
        debugPrint('Backend executable not found');
        _isStarting = false;
        return false;
      }

      debugPrint('Starting backend from: $backendPath');

      // Start the backend process
      _backendProcess = await Process.start(
        backendPath,
        [],
        workingDirectory: Directory(backendPath).parent.path,
      );

      // Wait a moment for the backend to start
      await Future.delayed(const Duration(seconds: 3));

      // Check if backend is responding
      _isRunning = await _isBackendAlreadyRunning();
      _isStarting = false;

      if (_isRunning) {
        debugPrint('Backend started successfully');
        
        // Listen to backend output
        _backendProcess!.stdout.listen((data) {
          debugPrint('Backend: ${String.fromCharCodes(data)}');
        });
        
        _backendProcess!.stderr.listen((data) {
          debugPrint('Backend Error: ${String.fromCharCodes(data)}');
        });
      } else {
        debugPrint('Backend failed to start');
        await stopBackend();
      }

      return _isRunning;
    } catch (e) {
      debugPrint('Error starting backend: $e');
      _isStarting = false;
      return false;
    }
  }

  Future<void> stopBackend() async {
    if (_backendProcess != null) {
      debugPrint('Stopping backend...');
      
      if (Platform.isWindows) {
        // On Windows, send Ctrl+C signal
        Process.run('taskkill', ['/PID', '${_backendProcess!.pid}', '/F']);
      } else {
        _backendProcess!.kill(ProcessSignal.sigterm);
      }
      
      await _backendProcess!.exitCode.timeout(
        const Duration(seconds: 5),
        onTimeout: () {
          _backendProcess!.kill(ProcessSignal.sigkill);
          return -1;
        },
      );
      
      _backendProcess = null;
      _isRunning = false;
      debugPrint('Backend stopped');
    }
  }

  Future<String> _findBackendExecutable() async {
    // Look for the backend executable in common locations
    List<String> possiblePaths = [];
    
    String currentDir = Directory.current.path;
    String executableDir = Platform.resolvedExecutable.replaceAll(Platform.pathSeparator + Platform.resolvedExecutable.split(Platform.pathSeparator).last, '');
    
    if (Platform.isWindows) {
      // Windows paths
      possiblePaths = [
        // Development paths
        '../InvoiceProcessor.Api/InvoiceProcessor.Api/bin/Debug/net8.0/InvoiceProcessor.Api.exe',
        '../InvoiceProcessor.Api/InvoiceProcessor.Api/bin/Release/net8.0/InvoiceProcessor.Api.exe',
        // Distribution paths
        '$executableDir/../backend/InvoiceProcessor.Api.exe',
        '$executableDir/backend/InvoiceProcessor.Api.exe',
        '$currentDir/backend/InvoiceProcessor.Api.exe',
        'backend/InvoiceProcessor.Api.exe',
        'InvoiceProcessor.Api.exe',
      ];
    } else {
      // Unix-like systems
      possiblePaths = [
        '../InvoiceProcessor.Api/InvoiceProcessor.Api/bin/Debug/net8.0/InvoiceProcessor.Api',
        '../InvoiceProcessor.Api/InvoiceProcessor.Api/bin/Release/net8.0/InvoiceProcessor.Api',
        '$executableDir/../backend/InvoiceProcessor.Api',
        '$executableDir/backend/InvoiceProcessor.Api',
        '$currentDir/backend/InvoiceProcessor.Api',
        'backend/InvoiceProcessor.Api',
        'InvoiceProcessor.Api',
      ];
    }
    
    for (String path in possiblePaths) {
      debugPrint('Checking for backend at: $path');
      if (await File(path).exists()) {
        debugPrint('Found backend at: $path');
        return path;
      }
    }

    debugPrint('Backend executable not found in any of the expected locations');
    return '';
  }

  Future<bool> _isBackendAlreadyRunning() async {
    try {
      final dio = Dio();
      dio.options.connectTimeout = const Duration(seconds: 2);
      dio.options.receiveTimeout = const Duration(seconds: 2);
      final response = await dio.get('http://localhost:5002/api/dashboard/stats');
      return response.statusCode == 200;
    } catch (e) {
      return false;
    }
  }

  Future<void> waitForBackend({int maxAttempts = 10}) async {
    for (int i = 0; i < maxAttempts; i++) {
      if (await _isBackendAlreadyRunning()) {
        _isRunning = true;
        return;
      }
      await Future.delayed(const Duration(seconds: 1));
    }
  }
}