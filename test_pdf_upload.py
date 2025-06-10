#!/usr/bin/env python3
import requests
import json
import os

def test_pdf_upload(pdf_path, invoice_type, description):
    """Test PDF upload to the invoice processing API"""
    
    url = "http://localhost:5002/api/invoices/upload"
    
    # Check if file exists
    if not os.path.exists(pdf_path):
        print(f"❌ File not found: {pdf_path}")
        return None
    
    print(f"\n🔍 Testing {description}")
    print(f"📁 File: {pdf_path}")
    print(f"📋 Type: {invoice_type}")
    
    try:
        # Prepare the multipart form data
        with open(pdf_path, 'rb') as pdf_file:
            files = {
                'file': (os.path.basename(pdf_path), pdf_file, 'application/pdf')
            }
            data = {
                'invoiceType': invoice_type
            }
            
            # Make the POST request
            response = requests.post(url, files=files, data=data, timeout=30)
            
        print(f"🌐 Response Status: {response.status_code}")
        
        if response.status_code == 200:
            result = response.json()
            print("✅ Upload Successful!")
            print(f"📄 Invoice ID: {result.get('id')}")
            print(f"📝 Type: {result.get('type')} ({get_type_name(result.get('type'))})")
            print(f"💰 Total Amount: {result.get('totalAmount')}")
            print(f"🎯 Confidence Score: {result.get('confidenceScore')}%")
            print(f"🏪 Supplier: {result.get('supplierName')}")
            print(f"📊 Status: {result.get('status')} ({get_status_name(result.get('status'))})")
            
            # Show items if any
            items = result.get('items', [])
            if items:
                print(f"\n📦 Parsed Items ({len(items)}):")
                for i, item in enumerate(items, 1):
                    print(f"  {i}. {item.get('productName')} - "
                          f"Qty: {item.get('quantity')} {item.get('unit')} - "
                          f"Price: {item.get('unitPrice')} - "
                          f"Total: {item.get('totalPrice')}")
            
            return result
        else:
            print(f"❌ Upload Failed!")
            try:
                error_data = response.json()
                print(f"Error: {error_data}")
            except:
                print(f"Error: {response.text}")
            return None
            
    except requests.exceptions.ConnectionError:
        print("❌ Connection Error: Make sure the backend API is running on http://localhost:5002")
        return None
    except Exception as e:
        print(f"❌ Unexpected error: {str(e)}")
        return None

def get_type_name(type_id):
    """Convert invoice type ID to readable name"""
    type_names = {1: "Purchase", 2: "Sale", 3: "Purchase Return", 4: "Sale Return"}
    return type_names.get(type_id, "Unknown")

def get_status_name(status_id):
    """Convert status ID to readable name"""
    status_names = {1: "Processing", 2: "Completed", 3: "Failed", 4: "Pending Review", 5: "Approved"}
    return status_names.get(status_id, "Unknown")

def get_stock_summary():
    """Get current stock summary"""
    try:
        response = requests.get("http://localhost:5002/api/stock/summary", timeout=10)
        if response.status_code == 200:
            data = response.json()
            print(f"\n📊 Stock Summary:")
            print(f"  Total Products: {data.get('totalProducts')}")
            print(f"  Low Stock Count: {data.get('lowStockCount')}")
            print(f"  Total Value: {data.get('totalValue')}")
        else:
            print("❌ Failed to get stock summary")
    except Exception as e:
        print(f"❌ Stock summary error: {e}")

def get_products():
    """Get all products"""
    try:
        response = requests.get("http://localhost:5002/api/products", timeout=10)
        if response.status_code == 200:
            products = response.json()
            print(f"\n📦 Products ({len(products)}):")
            for product in products:
                print(f"  • {product.get('name')} - Stock: {product.get('currentStock')} {product.get('defaultUnit')}")
        else:
            print("❌ Failed to get products")
    except Exception as e:
        print(f"❌ Products error: {e}")

def main():
    """Main test function"""
    print("🧪 PDF Invoice Processing Test")
    print("=" * 50)
    
    # Test file paths
    base_path = "/Users/sakinburakcivelek/flutter_and_c#/InvoinceStock"
    test_files = [
        (f"{base_path}/test-buy.pdf", "purchase", "Purchase Invoice Test"),
        (f"{base_path}/test-sell.pdf", "sale", "Sale Invoice Test")
    ]
    
    results = []
    
    # Test each PDF
    for pdf_path, invoice_type, description in test_files:
        result = test_pdf_upload(pdf_path, invoice_type, description)
        if result:
            results.append(result)
        print("-" * 50)
    
    # Show final results
    print(f"\n📋 Test Summary: {len(results)}/{len(test_files)} successful uploads")
    
    # Get stock summary
    get_stock_summary()
    get_products()

if __name__ == "__main__":
    main()