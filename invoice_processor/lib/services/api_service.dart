import 'dart:io';
import 'package:dio/dio.dart';
import '../models/invoice.dart';
import '../models/product.dart';

class ApiService {
  static const String baseUrl = 'http://localhost:5002/api';
  late final Dio _dio;

  ApiService() {
    _dio = Dio(BaseOptions(
      baseUrl: baseUrl,
      connectTimeout: const Duration(seconds: 30),
      receiveTimeout: const Duration(seconds: 60),
    ));
  }

  // Invoice endpoints
  Future<List<Invoice>> getInvoices() async {
    final response = await _dio.get('/invoices');
    return (response.data as List)
        .map((json) => Invoice.fromJson(json))
        .toList();
  }

  Future<Invoice> getInvoice(int id) async {
    final response = await _dio.get('/invoices/$id');
    return Invoice.fromJson(response.data);
  }

  Future<Invoice> uploadInvoice(File file) async {
    final formData = FormData.fromMap({
      'file': await MultipartFile.fromFile(
        file.path,
        filename: file.path.split('/').last,
      ),
    });

    final response = await _dio.post('/invoices/upload', data: formData);
    return Invoice.fromJson(response.data);
  }

  Future<Invoice> uploadInvoiceWithType(File file, String invoiceType) async {
    final formData = FormData.fromMap({
      'file': await MultipartFile.fromFile(
        file.path,
        filename: file.path.split('/').last,
      ),
      'invoiceType': invoiceType,
    });

    final response = await _dio.post('/invoices/upload', data: formData);
    return Invoice.fromJson(response.data);
  }

  Future<Invoice> approveInvoice(int id) async {
    final response = await _dio.post('/invoices/$id/approve');
    return Invoice.fromJson(response.data);
  }

  Future<Invoice> updateAndApproveInvoice(Invoice invoice) async {
    final response = await _dio.put('/invoices/${invoice.id}/update-and-approve', 
        data: invoice.toJson());
    return Invoice.fromJson(response.data);
  }

  Future<void> deleteInvoice(int id) async {
    await _dio.delete('/invoices/$id');
  }

  // Product endpoints
  Future<List<Product>> getProducts() async {
    final response = await _dio.get('/products');
    return (response.data as List)
        .map((json) => Product.fromJson(json))
        .toList();
  }

  Future<Product> createProduct(Product product) async {
    final response = await _dio.post('/products', data: product.toJson());
    return Product.fromJson(response.data);
  }

  Future<Product> updateProduct(Product product) async {
    final response =
        await _dio.put('/products/${product.id}', data: product.toJson());
    return Product.fromJson(response.data);
  }

  // Stock endpoints
  Future<List<StockMovement>> getStockMovements({int? productId}) async {
    String url = '/stock/movements';
    if (productId != null) url += '?productId=$productId';

    final response = await _dio.get(url);
    return (response.data as List)
        .map((json) => StockMovement.fromJson(json))
        .toList();
  }

  Future<Map<String, dynamic>> getStockSummary() async {
    final response = await _dio.get('/stock/summary');
    return response.data;
  }

  // Statistics
  Future<Map<String, dynamic>> getDashboardStats() async {
    final response = await _dio.get('/dashboard/stats');
    return response.data;
  }
}