class Product {
  final int id;
  final String name;
  final String? code;
  final String? description;
  final String? category;
  final String defaultUnit;
  final double currentStock;
  final double minimumStock;
  final double? lastPurchasePrice;
  final DateTime? lastUpdated;
  final DateTime createdDate;

  Product({
    required this.id,
    required this.name,
    this.code,
    this.description,
    this.category,
    required this.defaultUnit,
    required this.currentStock,
    required this.minimumStock,
    this.lastPurchasePrice,
    this.lastUpdated,
    required this.createdDate,
  });

  factory Product.fromJson(Map<String, dynamic> json) {
    return Product(
      id: json['id'],
      name: json['name'],
      code: json['code'],
      description: json['description'],
      category: json['category'],
      defaultUnit: json['defaultUnit'],
      currentStock: json['currentStock'].toDouble(),
      minimumStock: json['minimumStock'].toDouble(),
      lastPurchasePrice: json['lastPurchasePrice']?.toDouble(),
      lastUpdated: json['lastUpdated'] != null
          ? DateTime.parse(json['lastUpdated'])
          : null,
      createdDate: DateTime.parse(json['createdDate']),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'name': name,
      'code': code,
      'description': description,
      'category': category,
      'defaultUnit': defaultUnit,
      'currentStock': currentStock,
      'minimumStock': minimumStock,
      'lastPurchasePrice': lastPurchasePrice,
      'lastUpdated': lastUpdated?.toIso8601String(),
      'createdDate': createdDate.toIso8601String(),
    };
  }

  bool get isLowStock => currentStock <= minimumStock;
}

class StockMovement {
  final int id;
  final int productId;
  final int? invoiceId;
  final MovementType type;
  final double quantity;
  final double previousStock;
  final double newStock;
  final DateTime movementDate;
  final String? description;

  StockMovement({
    required this.id,
    required this.productId,
    this.invoiceId,
    required this.type,
    required this.quantity,
    required this.previousStock,
    required this.newStock,
    required this.movementDate,
    this.description,
  });

  factory StockMovement.fromJson(Map<String, dynamic> json) {
    return StockMovement(
      id: json['id'],
      productId: json['productId'],
      invoiceId: json['invoiceId'],
      type: MovementType.values[json['type'] - 1],
      quantity: json['quantity'].toDouble(),
      previousStock: json['previousStock'].toDouble(),
      newStock: json['newStock'].toDouble(),
      movementDate: DateTime.parse(json['movementDate']),
      description: json['description'],
    );
  }
}

enum MovementType {
  purchase,
  sale,
  purchaseReturn,
  saleReturn,
  adjustment,
}

extension MovementTypeExtension on MovementType {
  String get displayName {
    switch (this) {
      case MovementType.purchase:
        return 'Alış';
      case MovementType.sale:
        return 'Satış';
      case MovementType.purchaseReturn:
        return 'Alış İadesi';
      case MovementType.saleReturn:
        return 'Satış İadesi';
      case MovementType.adjustment:
        return 'Düzeltme';
    }
  }
}
