import 'dart:io';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:file_picker/file_picker.dart';
import '../providers/invoice_provider.dart';
import '../providers/stock_provider.dart';
import '../widgets/invoice_list.dart';
import '../widgets/stock_summary.dart';
import '../widgets/dashboard_stats.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  int _selectedIndex = 0;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _loadInitialData();
    });
  }

  void _loadInitialData() {
    context.read<InvoiceProvider>().loadInvoices();
    context.read<StockProvider>().loadProducts();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Row(
        children: [
          NavigationRail(
            selectedIndex: _selectedIndex,
            onDestinationSelected: (index) {
              setState(() => _selectedIndex = index);
            },
            labelType: NavigationRailLabelType.all,
            destinations: const [
              NavigationRailDestination(
                icon: Icon(Icons.dashboard),
                label: Text('Dashboard'),
              ),
              NavigationRailDestination(
                icon: Icon(Icons.receipt),
                label: Text('Faturalar'),
              ),
              NavigationRailDestination(
                icon: Icon(Icons.inventory),
                label: Text('Stok'),
              ),
              NavigationRailDestination(
                icon: Icon(Icons.upload_file),
                label: Text('Yükle'),
              ),
            ],
          ),
          const VerticalDivider(thickness: 1, width: 1),
          Expanded(
            child: _buildSelectedPage(),
          ),
        ],
      ),
    );
  }

  Widget _buildSelectedPage() {
    switch (_selectedIndex) {
      case 0:
        return const DashboardPage();
      case 1:
        return const InvoicesPage();
      case 2:
        return const StockPage();
      case 3:
        return const UploadPage();
      default:
        return const DashboardPage();
    }
  }
}

class DashboardPage extends StatelessWidget {
  const DashboardPage({super.key});

  @override
  Widget build(BuildContext context) {
    return const Padding(
      padding: EdgeInsets.all(24.0),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'Dashboard',
            style: TextStyle(fontSize: 32, fontWeight: FontWeight.bold),
          ),
          SizedBox(height: 24),
          Expanded(
            child: Row(
              children: [
                Expanded(flex: 2, child: DashboardStats()),
                SizedBox(width: 16),
                Expanded(child: StockSummary()),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class InvoicesPage extends StatelessWidget {
  const InvoicesPage({super.key});

  @override
  Widget build(BuildContext context) {
    return const Padding(
      padding: EdgeInsets.all(24.0),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'Faturalar',
            style: TextStyle(fontSize: 32, fontWeight: FontWeight.bold),
          ),
          SizedBox(height: 24),
          Expanded(child: InvoiceList()),
        ],
      ),
    );
  }
}

class StockPage extends StatelessWidget {
  const StockPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(24.0),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              const Text(
                'Stok Yönetimi',
                style: TextStyle(fontSize: 32, fontWeight: FontWeight.bold),
              ),
              ElevatedButton.icon(
                onPressed: () {
                  context.read<StockProvider>().loadProducts();
                },
                icon: const Icon(Icons.refresh),
                label: const Text('Yenile'),
              ),
            ],
          ),
          const SizedBox(height: 24),
          Expanded(
            child: Consumer<StockProvider>(
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
                        Text('Hata: ${provider.error}'),
                        const SizedBox(height: 16),
                        ElevatedButton(
                          onPressed: () => provider.loadProducts(),
                          child: const Text('Tekrar Dene'),
                        ),
                      ],
                    ),
                  );
                }

                if (provider.products.isEmpty) {
                  return const Center(
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Icon(Icons.inventory_2_outlined, size: 64, color: Colors.grey),
                        SizedBox(height: 16),
                        Text('Henüz ürün bulunmuyor'),
                        SizedBox(height: 8),
                        Text(
                          'Fatura yükleyip onayladıktan sonra ürünler burada görünecek',
                          style: TextStyle(color: Colors.grey),
                          textAlign: TextAlign.center,
                        ),
                      ],
                    ),
                  );
                }

                return ListView.builder(
                  itemCount: provider.products.length,
                  itemBuilder: (context, index) {
                    final product = provider.products[index];
                    final isLowStock = product.currentStock <= product.minimumStock;
                    
                    return Card(
                      color: isLowStock ? Colors.red.shade50 : null,
                      child: ListTile(
                        leading: Icon(
                          Icons.inventory,
                          color: isLowStock ? Colors.red : Colors.blue,
                        ),
                        title: Text(
                          product.name,
                          style: TextStyle(
                            fontWeight: FontWeight.w500,
                            color: isLowStock ? Colors.red.shade700 : null,
                          ),
                        ),
                        subtitle: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text('Stok: ${product.currentStock} ${product.defaultUnit}'),
                            if (product.code != null)
                              Text('Kod: ${product.code}', style: const TextStyle(fontSize: 12)),
                          ],
                        ),
                        trailing: Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          crossAxisAlignment: CrossAxisAlignment.end,
                          children: [
                            Text(
                              '₺${product.lastPurchasePrice?.toStringAsFixed(2) ?? '-'}',
                              style: const TextStyle(fontWeight: FontWeight.bold),
                            ),
                            if (isLowStock)
                              const Text(
                                'Düşük Stok!',
                                style: TextStyle(
                                  color: Colors.red,
                                  fontSize: 12,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                          ],
                        ),
                      ),
                    );
                  },
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}

class UploadPage extends StatefulWidget {
  const UploadPage({super.key});

  @override
  State<UploadPage> createState() => _UploadPageState();
}

class _UploadPageState extends State<UploadPage> {
  String _selectedInvoiceType = 'purchase'; // Default value
  File? _selectedFile;
  bool _isUploading = false;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(24.0),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Fatura Yükle',
            style: TextStyle(fontSize: 32, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 24),
          
          // Fatura türü seçimi
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16.0),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Fatura Türü Seçin',
                    style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                  ),
                  const SizedBox(height: 8),
                  const Text(
                    'Sizin perspektifinizden fatura türünü seçin:',
                    style: TextStyle(color: Colors.grey, fontSize: 14),
                  ),
                  const SizedBox(height: 16),
                  Row(
                    children: [
                      Expanded(
                        child: RadioListTile<String>(
                          title: const Text('Alış Faturası'),
                          subtitle: const Text('Ürün satın aldığınız fatura\n(Stok artacak)'),
                          value: 'purchase',
                          groupValue: _selectedInvoiceType,
                          onChanged: (value) {
                            if (value != null) {
                              setState(() {
                                _selectedInvoiceType = value;
                              });
                              debugPrint('Selected invoice type: $value');
                            }
                          },
                        ),
                      ),
                      Expanded(
                        child: RadioListTile<String>(
                          title: const Text('Satış Faturası'),
                          subtitle: const Text('Ürün sattığınız fatura\n(Stok azalacak)'),
                          value: 'sale',
                          groupValue: _selectedInvoiceType,
                          onChanged: (value) {
                            if (value != null) {
                              setState(() {
                                _selectedInvoiceType = value;
                              });
                              debugPrint('Selected invoice type: $value');
                            }
                          },
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),
          
          const SizedBox(height: 24),
          
          // Dosya seçimi
          Expanded(
            child: Center(
              child: Card(
                child: Container(
                  width: 500,
                  height: 350,
                  padding: const EdgeInsets.all(32),
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      if (_selectedFile == null) ...[
                        const Icon(Icons.upload_file,
                            size: 64, color: Colors.blue),
                        const SizedBox(height: 16),
                        const Text(
                          'Fatura dosyasını seçin',
                          style: TextStyle(fontSize: 18),
                        ),
                        const SizedBox(height: 8),
                        const Text(
                          'PDF, JPG, PNG, TIFF desteklenir',
                          style: TextStyle(color: Colors.grey),
                        ),
                        const SizedBox(height: 24),
                        ElevatedButton.icon(
                          onPressed: _isUploading ? null : _selectFile,
                          icon: const Icon(Icons.folder_open),
                          label: const Text('Dosya Seç'),
                        ),
                      ] else ...[
                        const Icon(Icons.description,
                            size: 64, color: Colors.green),
                        const SizedBox(height: 16),
                        Text(
                          'Seçilen Dosya:',
                          style: TextStyle(
                            fontSize: 16,
                            color: Colors.grey[600],
                          ),
                        ),
                        const SizedBox(height: 8),
                        Text(
                          _selectedFile!.path.split('/').last,
                          style: const TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.bold,
                          ),
                          textAlign: TextAlign.center,
                        ),
                        const SizedBox(height: 16),
                        Text(
                          'Fatura Türü: ${_getInvoiceTypeDisplay(_selectedInvoiceType)}',
                          style: const TextStyle(
                            fontSize: 16,
                            color: Colors.blue,
                            fontWeight: FontWeight.w500,
                          ),
                        ),
                        const SizedBox(height: 24),
                        if (_isUploading) ...[
                          const CircularProgressIndicator(),
                          const SizedBox(height: 16),
                          const Text('Fatura işleniyor...'),
                        ] else ...[
                          Row(
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: [
                              ElevatedButton.icon(
                                onPressed: _uploadFile,
                                icon: const Icon(Icons.upload),
                                label: const Text('Yükle ve İşle'),
                                style: ElevatedButton.styleFrom(
                                  backgroundColor: Colors.green,
                                  foregroundColor: Colors.white,
                                ),
                              ),
                              const SizedBox(width: 16),
                              TextButton.icon(
                                onPressed: () {
                                  setState(() {
                                    _selectedFile = null;
                                  });
                                },
                                icon: const Icon(Icons.clear),
                                label: const Text('İptal'),
                              ),
                            ],
                          ),
                        ],
                      ],
                    ],
                  ),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  String _getInvoiceTypeDisplay(String type) {
    switch (type) {
      case 'purchase':
        return 'Alış Faturası (Stok artacak)';
      case 'sale':
        return 'Satış Faturası (Stok azalacak)';
      default:
        return type;
    }
  }

  void _selectFile() async {
    try {
      debugPrint('Dosya seçici açılıyor...');
      final result = await FilePicker.platform.pickFiles(
        type: FileType.custom,
        allowedExtensions: ['pdf', 'jpg', 'jpeg', 'png', 'tiff'],
        allowMultiple: false,
      );

      debugPrint('Dosya seçici sonucu: $result');
      
      if (result != null && result.files.isNotEmpty) {
        final file = result.files.single;
        debugPrint('Seçilen dosya: ${file.name}, path: ${file.path}');
        
        if (file.path != null) {
          final selectedFile = File(file.path!);
          
          if (await selectedFile.exists()) {
            debugPrint('Dosya mevcut: ${selectedFile.path}');
            setState(() {
              _selectedFile = selectedFile;
            });
          } else {
            debugPrint('Dosya bulunamadı: ${selectedFile.path}');
            _showErrorMessage('Seçilen dosya bulunamadı.');
          }
        } else {
          debugPrint('Dosya yolu null');
          _showErrorMessage('Dosya yolu alınamadı.');
        }
      } else {
        debugPrint('Dosya seçilmedi');
      }
    } catch (e) {
      debugPrint('Dosya seçiminde hata: $e');
      _showErrorMessage('Dosya seçiminde hata: $e');
    }
  }

  void _showErrorMessage(String message) {
    if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(message),
          backgroundColor: Colors.red,
        ),
      );
    }
  }

  void _uploadFile() async {
    if (_selectedFile == null) {
      _showErrorMessage('Lütfen dosya seçin');
      return;
    }

    if (_selectedInvoiceType.isEmpty) {
      _showErrorMessage('Lütfen fatura türü seçin');
      return;
    }

    setState(() {
      _isUploading = true;
    });

    try {
      debugPrint('Dosya yükleniyor: ${_selectedFile!.path}');
      debugPrint('Fatura türü: $_selectedInvoiceType');
      
      if (context.mounted) {
        await context.read<InvoiceProvider>().uploadInvoiceWithType(
          _selectedFile!,
          _selectedInvoiceType,
        );
        
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text('Fatura başarıyla yüklendi! ($_selectedInvoiceType)'),
              backgroundColor: Colors.green,
            ),
          );
        }
        
        // Reset form
        setState(() {
          _selectedFile = null;
          _isUploading = false;
          // Keep the selected invoice type for convenience
        });
      }
    } catch (e) {
      debugPrint('Dosya yükleme hatası: $e');
      setState(() {
        _isUploading = false;
      });
      
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Hata: $e'),
            backgroundColor: Colors.red,
          ),
        );
      }
    }
  }
}