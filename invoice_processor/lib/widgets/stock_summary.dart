import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/stock_provider.dart';

class StockSummary extends StatelessWidget {
  const StockSummary({super.key});

  @override
  Widget build(BuildContext context) {
    return Consumer<StockProvider>(
      builder: (context, provider, child) {
        final lowStockProducts = provider.lowStockProducts;

        return Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'Düşük Stok Uyarısı',
                  style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                ),
                const SizedBox(height: 16),
                if (lowStockProducts.isEmpty)
                  const Center(
                    child: Text(
                      'Düşük stoklu ürün yok',
                      style: TextStyle(color: Colors.grey),
                    ),
                  )
                else
                  Expanded(
                    child: ListView.builder(
                      itemCount: lowStockProducts.length,
                      itemBuilder: (context, index) {
                        final product = lowStockProducts[index];
                        return ListTile(
                          leading: const Icon(Icons.warning, color: Colors.red),
                          title: Text(product.name),
                          subtitle: Text(
                            'Mevcut: ${product.currentStock} ${product.defaultUnit}',
                          ),
                          trailing: Text(
                            'Min: ${product.minimumStock}',
                            style: const TextStyle(color: Colors.red),
                          ),
                        );
                      },
                    ),
                  ),
              ],
            ),
          ),
        );
      },
    );
  }
}
