import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../models/invoice.dart';
import '../providers/invoice_provider.dart';
import '../providers/stock_provider.dart';

class InvoiceReviewPage extends StatefulWidget {
  final Invoice invoice;

  const InvoiceReviewPage({super.key, required this.invoice});

  @override
  State<InvoiceReviewPage> createState() => _InvoiceReviewPageState();
}

class _InvoiceReviewPageState extends State<InvoiceReviewPage> {
  late List<InvoiceItemEdit> editableItems;
  bool _isLoading = false;

  @override
  void initState() {
    super.initState();
    editableItems = widget.invoice.items
        .map((item) => InvoiceItemEdit.fromInvoiceItem(item))
        .toList();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Fatura İnceleme'),
        actions: [
          TextButton.icon(
            onPressed: _isLoading ? null : _approveInvoice,
            icon: const Icon(Icons.check, color: Colors.white),
            label: const Text('Onayla', style: TextStyle(color: Colors.white)),
          ),
        ],
      ),
      body: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16.0),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Fatura Bilgileri',
                      style: Theme.of(context).textTheme.titleLarge,
                    ),
                    const SizedBox(height: 8),
                    Text('Dosya: ${widget.invoice.fileName}'),
                    Text('Tip: ${widget.invoice.type.displayName}'),
                    Text('Toplam: ₺${widget.invoice.totalAmount.toStringAsFixed(2)}'),
                    Text('Güven Skoru: %${widget.invoice.confidenceScore}'),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 16),
            Text(
              'Ürünler (${editableItems.length} adet)',
              style: Theme.of(context).textTheme.titleLarge,
            ),
            const SizedBox(height: 8),
            Expanded(
              child: ListView.builder(
                itemCount: editableItems.length,
                itemBuilder: (context, index) {
                  return _buildEditableItem(index);
                },
              ),
            ),
            if (_isLoading)
              const Center(child: CircularProgressIndicator()),
          ],
        ),
      ),
    );
  }

  Widget _buildEditableItem(int index) {
    final item = editableItems[index];
    
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: Padding(
        padding: const EdgeInsets.all(12.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: TextFormField(
                    initialValue: item.productName,
                    decoration: const InputDecoration(
                      labelText: 'Ürün Adı',
                      border: OutlineInputBorder(),
                    ),
                    onChanged: (value) {
                      setState(() {
                        item.productName = value;
                      });
                    },
                  ),
                ),
                const SizedBox(width: 8),
                IconButton(
                  onPressed: () => _removeItem(index),
                  icon: const Icon(Icons.delete, color: Colors.red),
                ),
              ],
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                Expanded(
                  flex: 2,
                  child: TextFormField(
                    initialValue: item.quantity.toString(),
                    decoration: const InputDecoration(
                      labelText: 'Miktar',
                      border: OutlineInputBorder(),
                    ),
                    keyboardType: TextInputType.number,
                    onChanged: (value) {
                      setState(() {
                        item.quantity = double.tryParse(value) ?? 1;
                      });
                    },
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  flex: 2,
                  child: DropdownButtonFormField<String>(
                    value: item.unit,
                    decoration: const InputDecoration(
                      labelText: 'Birim',
                      border: OutlineInputBorder(),
                    ),
                    items: const [
                      DropdownMenuItem(value: 'adet', child: Text('Adet')),
                      DropdownMenuItem(value: 'kg', child: Text('Kilogram')),
                      DropdownMenuItem(value: 'litre', child: Text('Litre')),
                      DropdownMenuItem(value: 'metre', child: Text('Metre')),
                      DropdownMenuItem(value: 'cm', child: Text('Santimetre')),
                      DropdownMenuItem(value: 'gram', child: Text('Gram')),
                      DropdownMenuItem(value: 'ton', child: Text('Ton')),
                    ],
                    onChanged: (value) {
                      if (value != null) {
                        setState(() {
                          item.unit = value;
                        });
                      }
                    },
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  flex: 3,
                  child: TextFormField(
                    initialValue: item.unitPrice.toStringAsFixed(2),
                    decoration: const InputDecoration(
                      labelText: 'Birim Fiyat (₺)',
                      border: OutlineInputBorder(),
                    ),
                    keyboardType: TextInputType.number,
                    onChanged: (value) {
                      setState(() {
                        item.unitPrice = double.tryParse(value) ?? 0;
                        item.totalPrice = item.quantity * item.unitPrice;
                      });
                    },
                  ),
                ),
              ],
            ),
            const SizedBox(height: 8),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  'Toplam: ₺${item.totalPrice.toStringAsFixed(2)}',
                  style: const TextStyle(fontWeight: FontWeight.bold),
                ),
                Consumer<StockProvider>(
                  builder: (context, stockProvider, child) {
                    final existingProduct = stockProvider.products
                        .where((p) => p.name.toLowerCase() == item.productName.toLowerCase())
                        .firstOrNull;
                    
                    if (existingProduct != null) {
                      return Container(
                        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                        decoration: BoxDecoration(
                          color: Colors.orange.shade100,
                          borderRadius: BorderRadius.circular(4),
                        ),
                        child: Text(
                          'Mevcut stok: ${existingProduct.currentStock} ${existingProduct.defaultUnit}',
                          style: TextStyle(
                            fontSize: 12,
                            color: Colors.orange.shade700,
                          ),
                        ),
                      );
                    }
                    return const SizedBox.shrink();
                  },
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  void _removeItem(int index) {
    setState(() {
      editableItems.removeAt(index);
    });
  }

  void _approveInvoice() async {
    if (editableItems.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('En az bir ürün olmalı'),
          backgroundColor: Colors.red,
        ),
      );
      return;
    }

    setState(() {
      _isLoading = true;
    });

    try {
      // Update invoice items with edited data
      final updatedItems = editableItems.map((edit) => InvoiceItem(
        id: edit.id,
        productName: edit.productName.trim(),
        productCode: edit.productCode,
        quantity: edit.quantity,
        unit: edit.unit,
        unitPrice: edit.unitPrice,
        totalPrice: edit.totalPrice,
        confidenceScore: edit.confidenceScore,
      )).toList();

      // Create updated invoice
      final updatedInvoice = Invoice(
        id: widget.invoice.id,
        fileName: widget.invoice.fileName,
        type: widget.invoice.type,
        processedDate: widget.invoice.processedDate,
        invoiceDate: widget.invoice.invoiceDate,
        invoiceNumber: widget.invoice.invoiceNumber,
        supplierName: widget.invoice.supplierName,
        customerName: widget.invoice.customerName,
        totalAmount: editableItems.fold(0, (sum, item) => sum + item.totalPrice),
        vatAmount: widget.invoice.vatAmount,
        status: ProcessingStatus.approved,
        confidenceScore: widget.invoice.confidenceScore,
        items: updatedItems,
      );

      if (context.mounted) {
        await context.read<InvoiceProvider>().approveInvoiceWithUpdates(updatedInvoice);
        await context.read<StockProvider>().loadProducts();
        
        Navigator.of(context).pop();
        
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Fatura onaylandı ve stok güncellendi'),
            backgroundColor: Colors.green,
          ),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Hata: $e'),
            backgroundColor: Colors.red,
          ),
        );
      }
    } finally {
      if (mounted) {
        setState(() {
          _isLoading = false;
        });
      }
    }
  }
}

class InvoiceItemEdit {
  int id;
  String productName;
  String? productCode;
  double quantity;
  String unit;
  double unitPrice;
  double totalPrice;
  int confidenceScore;

  InvoiceItemEdit({
    required this.id,
    required this.productName,
    this.productCode,
    required this.quantity,
    required this.unit,
    required this.unitPrice,
    required this.totalPrice,
    required this.confidenceScore,
  });

  factory InvoiceItemEdit.fromInvoiceItem(InvoiceItem item) {
    return InvoiceItemEdit(
      id: item.id,
      productName: item.productName,
      productCode: item.productCode,
      quantity: item.quantity,
      unit: item.unit,
      unitPrice: item.unitPrice,
      totalPrice: item.totalPrice,
      confidenceScore: item.confidenceScore,
    );
  }
}