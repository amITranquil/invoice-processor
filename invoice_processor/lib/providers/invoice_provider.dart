import 'dart:io';
import 'package:flutter/material.dart';
import '../models/invoice.dart';
import '../services/api_service.dart';

class InvoiceProvider extends ChangeNotifier {
  final ApiService _apiService;

  List<Invoice> _invoices = [];
  bool _isLoading = false;
  String? _error;

  List<Invoice> get invoices => _invoices;
  bool get isLoading => _isLoading;
  String? get error => _error;

  InvoiceProvider(this._apiService);

  Future<void> loadInvoices() async {
    _setLoading(true);
    try {
      _invoices = await _apiService.getInvoices();
      _error = null;
    } catch (e) {
      _error = e.toString();
    } finally {
      _setLoading(false);
    }
  }

  Future<void> uploadInvoice(File file) async {
    _setLoading(true);
    try {
      final invoice = await _apiService.uploadInvoice(file);
      _invoices.insert(0, invoice);
      _error = null;
    } catch (e) {
      _error = e.toString();
    } finally {
      _setLoading(false);
    }
  }

  Future<void> approveInvoice(int id) async {
    _setLoading(true);
    try {
      final updatedInvoice = await _apiService.approveInvoice(id);
      final index = _invoices.indexWhere((i) => i.id == id);
      if (index != -1) {
        _invoices[index] = updatedInvoice;
      }
      _error = null;
    } catch (e) {
      _error = e.toString();
    } finally {
      _setLoading(false);
    }
  }

  Future<void> deleteInvoice(int id) async {
    _setLoading(true);
    try {
      await _apiService.deleteInvoice(id);
      _invoices.removeWhere((i) => i.id == id);
      _error = null;
    } catch (e) {
      _error = e.toString();
    } finally {
      _setLoading(false);
    }
  }

  void _setLoading(bool loading) {
    _isLoading = loading;
    notifyListeners();
  }
}
