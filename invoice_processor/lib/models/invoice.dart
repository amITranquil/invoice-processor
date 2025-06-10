class Invoice {
  final int id;
  final String fileName;
  final InvoiceType type;
  final DateTime processedDate;
  final DateTime? invoiceDate;
  final String? invoiceNumber;
  final String? supplierName;
  final String? customerName;
  final double totalAmount;
  final double? vatAmount;
  final ProcessingStatus status;
  final int confidenceScore;
  final String? errorMessage;
  final List<InvoiceItem> items;

  Invoice({
    required this.id,
    required this.fileName,
    required this.type,
    required this.processedDate,
    this.invoiceDate,
    this.invoiceNumber,
    this.supplierName,
    this.customerName,
    required this.totalAmount,
    this.vatAmount,
    required this.status,
    required this.confidenceScore,
    this.errorMessage,
    this.items = const [],
  });

  factory Invoice.fromJson(Map<String, dynamic> json) {
    return Invoice(
      id: json['id'],
      fileName: json['fileName'],
      type: InvoiceType.values[json['type'] - 1],
      processedDate: DateTime.parse(json['processedDate']),
      invoiceDate: json['invoiceDate'] != null
          ? DateTime.parse(json['invoiceDate'])
          : null,
      invoiceNumber: json['invoiceNumber'],
      supplierName: json['supplierName'],
      customerName: json['customerName'],
      totalAmount: json['totalAmount'].toDouble(),
      vatAmount: json['vatAmount']?.toDouble(),
      status: ProcessingStatus.values[json['status'] - 1],
      confidenceScore: json['confidenceScore'],
      errorMessage: json['errorMessage'],
      items: (json['items'] as List?)
              ?.map((item) => InvoiceItem.fromJson(item))
              .toList() ??
          [],
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'fileName': fileName,
      'type': type.index + 1,
      'processedDate': processedDate.toIso8601String(),
      'invoiceDate': invoiceDate?.toIso8601String(),
      'invoiceNumber': invoiceNumber,
      'supplierName': supplierName,
      'customerName': customerName,
      'totalAmount': totalAmount,
      'vatAmount': vatAmount,
      'status': status.index + 1,
      'confidenceScore': confidenceScore,
      'errorMessage': errorMessage,
      'items': items.map((item) => item.toJson()).toList(),
    };
  }
}

class InvoiceItem {
  final int id;
  final String productName;
  final String? productCode;
  final double quantity;
  final String unit;
  final double unitPrice;
  final double totalPrice;
  final double? vatRate;
  final int confidenceScore;

  InvoiceItem({
    required this.id,
    required this.productName,
    this.productCode,
    required this.quantity,
    required this.unit,
    required this.unitPrice,
    required this.totalPrice,
    this.vatRate,
    required this.confidenceScore,
  });

  factory InvoiceItem.fromJson(Map<String, dynamic> json) {
    return InvoiceItem(
      id: json['id'],
      productName: json['productName'],
      productCode: json['productCode'],
      quantity: json['quantity'].toDouble(),
      unit: json['unit'],
      unitPrice: json['unitPrice'].toDouble(),
      totalPrice: json['totalPrice'].toDouble(),
      vatRate: json['vatRate']?.toDouble(),
      confidenceScore: json['confidenceScore'],
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'productName': productName,
      'productCode': productCode,
      'quantity': quantity,
      'unit': unit,
      'unitPrice': unitPrice,
      'totalPrice': totalPrice,
      'vatRate': vatRate,
      'confidenceScore': confidenceScore,
    };
  }
}

enum InvoiceType {
  purchase,
  sale,
  purchaseReturn,
  saleReturn,
}

enum ProcessingStatus {
  processing,
  completed,
  failed,
  pendingReview,
  approved,
}

extension InvoiceTypeExtension on InvoiceType {
  String get displayName {
    switch (this) {
      case InvoiceType.purchase:
        return 'Alış';
      case InvoiceType.sale:
        return 'Satış';
      case InvoiceType.purchaseReturn:
        return 'Alış İadesi';
      case InvoiceType.saleReturn:
        return 'Satış İadesi';
    }
  }
}

extension ProcessingStatusExtension on ProcessingStatus {
  String get displayName {
    switch (this) {
      case ProcessingStatus.processing:
        return 'İşleniyor';
      case ProcessingStatus.completed:
        return 'Tamamlandı';
      case ProcessingStatus.failed:
        return 'Başarısız';
      case ProcessingStatus.pendingReview:
        return 'İnceleme Bekliyor';
      case ProcessingStatus.approved:
        return 'Onaylandı';
    }
  }
}
