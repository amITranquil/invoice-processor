import 'package:flutter/material.dart';
import '../models/product.dart';
import '../services/api_service.dart';

class StockProvider extends ChangeNotifier {
  final ApiService _apiService;

  List<Product> _products = [];
  List<StockMovement> _movements = [];
  bool _isLoading = false;
  String? _error;

  List<Product> get products => _products;
  List<StockMovement> get movements => _movements;
  bool get isLoading => _isLoading;
  String? get error => _error;

  StockProvider(this._apiService);

  Future<void> loadProducts() async {
    _setLoading(true);
    try {
      _products = await _apiService.getProducts();
      _error = null;
    } catch (e) {
      _error = e.toString();
    } finally {
      _setLoading(false);
    }
  }

  Future<void> loadMovements({int? productId}) async {
    _setLoading(true);
    try {
      _movements = await _apiService.getStockMovements(productId: productId);
      _error = null;
    } catch (e) {
      _error = e.toString();
    } finally {
      _setLoading(false);
    }
  }

  Future<void> createProduct(Product product) async {
    _setLoading(true);
    try {
      final newProduct = await _apiService.createProduct(product);
      _products.add(newProduct);
      _error = null;
    } catch (e) {
      _error = e.toString();
    } finally {
      _setLoading(false);
    }
  }

  List<Product> get lowStockProducts =>
      _products.where((p) => p.isLowStock).toList();

  void _setLoading(bool loading) {
    _isLoading = loading;
    notifyListeners();
  }
}
