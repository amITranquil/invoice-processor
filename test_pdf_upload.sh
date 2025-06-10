#!/bin/bash

echo "🧪 PDF Invoice Processing Test"
echo "================================"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# API URL
API_URL="http://localhost:5002/api/invoices/upload"

# Test files
BASE_PATH="/Users/sakinburakcivelek/flutter_and_c#/InvoinceStock"
BUY_PDF="$BASE_PATH/test-buy.pdf"
SELL_PDF="$BASE_PATH/test-sell.pdf"

# Function to test PDF upload
test_pdf_upload() {
    local pdf_file="$1"
    local invoice_type="$2"
    local description="$3"
    
    echo -e "\n${BLUE}🔍 Testing: $description${NC}"
    echo -e "📁 File: $(basename "$pdf_file")"
    echo -e "📋 Type: $invoice_type"
    
    if [ ! -f "$pdf_file" ]; then
        echo -e "${RED}❌ File not found: $pdf_file${NC}"
        return 1
    fi
    
    # Make the upload request
    response=$(curl -s -w "HTTPSTATUS:%{http_code}" \
        -X POST "$API_URL" \
        -F "file=@$pdf_file" \
        -F "invoiceType=$invoice_type")
    
    # Extract HTTP status and body
    http_status=$(echo "$response" | grep -o "HTTPSTATUS:[0-9]*" | cut -d: -f2)
    body=$(echo "$response" | sed -E 's/HTTPSTATUS:[0-9]*$//')
    
    echo -e "🌐 HTTP Status: $http_status"
    
    if [ "$http_status" = "200" ]; then
        echo -e "${GREEN}✅ Upload Successful!${NC}"
        
        # Parse JSON response (basic parsing)
        echo "$body" | python3 -m json.tool 2>/dev/null || echo "$body"
        
        return 0
    else
        echo -e "${RED}❌ Upload Failed!${NC}"
        echo -e "Response: $body"
        return 1
    fi
}

# Function to get stock summary
get_stock_summary() {
    echo -e "\n${YELLOW}📊 Getting Stock Summary...${NC}"
    
    response=$(curl -s "http://localhost:5002/api/stock/summary")
    echo "$response" | python3 -m json.tool 2>/dev/null || echo "$response"
}

# Function to get products
get_products() {
    echo -e "\n${YELLOW}📦 Getting Products...${NC}"
    
    response=$(curl -s "http://localhost:5002/api/products")
    echo "$response" | python3 -m json.tool 2>/dev/null || echo "$response"
}

# Function to check if API is running
check_api() {
    echo -e "${BLUE}🔍 Checking if API is running...${NC}"
    
    response=$(curl -s -w "HTTPSTATUS:%{http_code}" "http://localhost:5002/api/dashboard/stats")
    http_status=$(echo "$response" | grep -o "HTTPSTATUS:[0-9]*" | cut -d: -f2)
    
    if [ "$http_status" = "200" ]; then
        echo -e "${GREEN}✅ API is running on http://localhost:5002${NC}"
        return 0
    else
        echo -e "${RED}❌ API is not running. Please start the backend first.${NC}"
        echo -e "Run: cd InvoiceProcessor.Api/InvoiceProcessor.Api && dotnet run"
        return 1
    fi
}

# Main execution
main() {
    # Check if API is running
    if ! check_api; then
        exit 1
    fi
    
    echo -e "\n${BLUE}📄 Starting PDF Tests...${NC}"
    
    success_count=0
    total_tests=2
    
    # Test purchase invoice
    if test_pdf_upload "$BUY_PDF" "purchase" "Purchase Invoice (Alış Faturası)"; then
        ((success_count++))
    fi
    
    echo -e "\n" + "="*50
    
    # Test sale invoice  
    if test_pdf_upload "$SELL_PDF" "sale" "Sale Invoice (Satış Faturası)"; then
        ((success_count++))
    fi
    
    # Summary
    echo -e "\n${YELLOW}📋 Test Summary: $success_count/$total_tests successful uploads${NC}"
    
    # Get current state
    get_stock_summary
    get_products
    
    echo -e "\n${GREEN}🎉 Test completed!${NC}"
}

# Run the main function
main