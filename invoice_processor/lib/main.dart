import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:window_manager/window_manager.dart';
import 'providers/invoice_provider.dart';
import 'providers/stock_provider.dart';
import 'screens/home_screen.dart';
import 'services/api_service.dart';
import 'services/backend_service.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();

  // Desktop window setup
  await windowManager.ensureInitialized();
  await windowManager.setTitle('Fatura İşleme ve Stok Yönetimi');
  await windowManager.setMinimumSize(const Size(1200, 800));

  // Start backend service
  debugPrint('Starting backend service...');
  bool backendStarted = await BackendService.instance.startBackend();
  if (backendStarted) {
    debugPrint('Backend service started successfully');
  } else {
    debugPrint('Failed to start backend service');
  }

  // Setup app lifecycle listener for backend cleanup
  WidgetsBinding.instance.addObserver(_AppLifecycleObserver());

  runApp(const InvoiceProcessorApp());
}

class _AppLifecycleObserver extends WidgetsBindingObserver {
  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.detached) {
      // App is closing, stop the backend
      BackendService.instance.stopBackend();
    }
  }
}

class InvoiceProcessorApp extends StatefulWidget {
  const InvoiceProcessorApp({super.key});

  @override
  State<InvoiceProcessorApp> createState() => _InvoiceProcessorAppState();
}

class _InvoiceProcessorAppState extends State<InvoiceProcessorApp> with WindowListener {
  @override
  void initState() {
    super.initState();
    windowManager.addListener(this);
  }

  @override
  void dispose() {
    windowManager.removeListener(this);
    super.dispose();
  }

  @override
  void onWindowClose() async {
    // Stop the backend when window is closing
    await BackendService.instance.stopBackend();
    await windowManager.destroy();
  }

  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider(create: (_) => InvoiceProvider(ApiService())),
        ChangeNotifierProvider(create: (_) => StockProvider(ApiService())),
      ],
      child: MaterialApp(
        title: 'Fatura İşleme Sistemi',
        theme: ThemeData(
          primarySwatch: Colors.blue,
          useMaterial3: true,
          cardTheme: CardTheme(
            elevation: 2,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(12),
            ),
          ),
        ),
        home: const HomeScreen(),
        debugShowCheckedModeBanner: false,
      ),
    );
  }
}
