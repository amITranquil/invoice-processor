import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/invoice_provider.dart';
import '../models/invoice.dart';


class InvoiceList extends StatelessWidget {
  const InvoiceList({super.key});

  @override
  Widget build(BuildContext context) {
    return Consumer<InvoiceProvider>(
      builder: (context, provider, child) {
        if (provider.isLoading) {
          return const Center(child: CircularProgressIndicator());
        }

        if (provider.error != null) {
          return Center(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                const Icon(Icons.error, size: 64, color: Colors.red),
                const SizedBox(height: 16),
                SelectableText('Hata: ${provider.error}'),
                const SizedBox(height: 16),
                ElevatedButton(
                  onPressed: provider.loadInvoices,
                  child: const Text('Tekrar Dene'),
                ),
              ],
            ),
          );
        }

        final invoices = provider.invoices;

        if (invoices.isEmpty) {
          return const Center(
            child: Text(
              'Henüz fatura yüklenmemiş',
              style: TextStyle(color: Colors.grey),
            ),
          );
        }

        return ListView.builder(
          itemCount: invoices.length,
          itemBuilder: (context, index) {
            final invoice = invoices[index];
            return Card(
              child: ListTile(
                leading: Icon(
                  _getInvoiceIcon(invoice.type),
                  color: _getStatusColor(invoice.status),
                ),
                title: Text(invoice.fileName),
                subtitle: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                        '${invoice.type.displayName} - ${invoice.status.displayName}'),
                    Text('₺${invoice.totalAmount.toStringAsFixed(2)}'),
                  ],
                ),
                trailing: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text('${invoice.confidenceScore}%'),
                    const SizedBox(width: 8),
                    if (invoice.status == ProcessingStatus.pendingReview)
                      IconButton(
                        onPressed: () => provider.approveInvoice(invoice.id),
                        icon: const Icon(Icons.check, color: Colors.green),
                      ),
                    IconButton(
                      onPressed: () => provider.deleteInvoice(invoice.id),
                      icon: const Icon(Icons.delete, color: Colors.red),
                    ),
                  ],
                ),
              ),
            );
          },
        );
      },
    );
  }

  IconData _getInvoiceIcon(InvoiceType type) {
    switch (type) {
      case InvoiceType.purchase:
        return Icons.shopping_cart;
      case InvoiceType.sale:
        return Icons.sell;
      case InvoiceType.purchaseReturn:
        return Icons.keyboard_return;
      case InvoiceType.saleReturn:
        return Icons.undo;
    }
  }

  Color _getStatusColor(ProcessingStatus status) {
    switch (status) {
      case ProcessingStatus.processing:
        return Colors.blue;
      case ProcessingStatus.completed:
        return Colors.green;
      case ProcessingStatus.failed:
        return Colors.red;
      case ProcessingStatus.pendingReview:
        return Colors.orange;
      case ProcessingStatus.approved:
        return Colors.green;
    }
  }
}
