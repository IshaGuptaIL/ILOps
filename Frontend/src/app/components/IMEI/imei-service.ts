import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environments.development';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';


export interface ApiResposne {
  message: string;
  result: any;
  activity?: any;
  count?: number;
  statusCode?: number;
  success: boolean;
}

export interface PO {
  poNumber: string;
  poId: number;
  poItemId: number;
  vendor: string;
  whse: string;
  part: string;
  ordQty: number;
  unitCost: number;
  guid: string;
}

export interface IMEIItem {
  imei: string;
  invalid?: boolean;
  dupe?: boolean;
}

export interface RecieveIMEIBO {
  PONumber: number;
  RecNo: number;
  Whse: string;
  PartNo: string;
  GUID: string;
  Vendor: string;
  Location: string;
  IMEI: string;
  XLSRow: number;
}


// new IMEI Code
export interface PurchaseOrderListItem {
  id: string;              // POITEMID — Col(14)
  purchaseOrderId: number; // POID — Col(13)
  poNumber: string;        // po_number — Col(0)
  vendor: string;          // vendor_no — Col(1)
  sequence: number;        // sequence/recno — Col(2)
  whse: string;            // whse — Col(3)
  partNo: string;          // part_no — Col(4)
  description: string;     // description — Col(5)
  guid: string;            // guid — Col(6)
  orderQty: number;        // order_qty — Col(7)
  receivedQty: number;     // received_qty — Col(8)
  unitCost: number;        // unit_price — Col(9)
  status: string;          // PO header status — Col(10)
  location: string;        // whse_location — Col(12)
}

export interface CheckErrorsRequest {
  purchaseOrderId: number;
  purchaseOrderLineId: string;
  packingSlipImeis: string[];
  scanListImeis: string[];
  isReversal: boolean;
  // These come from Combo3 selection — needed for qty validation
  orderQty: number;
  receivedQty: number;
  whse: string;
}

export interface CheckErrorsResponse {
  hasErrors: boolean;
  errors: string[];
  packingSlipCount: number;
  scanListCount: number;
  invalidScanCount: number;
  invalidPackCount: number;
  scanDupeCount: number;
  packDupeCount: number;
  matches: string[];
  scanNoPack: string[];
  packNoScan: string[];
  alreadyInInventory: string[];
  invalidScanImeis: string[];
  invalidPackImeis: string[];
}

export interface ReceiveImeiRequest {
  purchaseOrderId: number;
  purchaseOrderLineId: string;
  imeis: string[];
  postReceipt: boolean;
  isReversal: boolean;
  cmoNumber: string;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
}

// new IMEI Code


@Injectable({
  providedIn: 'root',
})
export class ImeiService {
  private ApiUrl = environment.apiUrl;
  
   private readonly RecieveImeiUrl = environment.apiUrl + '/RecieveImei';
   private readonly NewRecieveUrl = environment.apiUrl + '/Hardware';



  constructor(private http: HttpClient) {}

  findByImei(imei: string): Observable<any> {
    return this.http.get<any>(
      `${this.ApiUrl}/Imei/find`,
      { params: { imei } }
    );
  }

  getRogersInvoices(bvReceiptNo: string): Observable<any> {
    return this.http.get<any>(
      `${this.ApiUrl}/Imei/rogers-invoices`,
      { params: { bvReceiptNo } }
    );
  }

// RecieveIMEI 
 // Get Purchase Orders
  getPurchaseOrders(): Observable<ApiResposne> {
    return this.http.get<ApiResposne>(`${this.RecieveImeiUrl}/GetPurchaseOrdersAsync`);
  }

  // Get IMEI Grids
  getIMEIGrids(poNumber: string): Observable<ApiResposne> {
    return this.http.get<ApiResposne>(`${this.RecieveImeiUrl}/GetIMEIGrids/${poNumber}`);
  }

  // Check Errors
  checkErrors(poId: number, poItemId: number, isReversal: boolean): Observable<ApiResposne> {
    let params = new HttpParams()
      .set('poId', poId.toString())
      .set('poItemId', poItemId.toString())
      .set('isReversal', isReversal.toString());

    return this.http.get<ApiResposne>(`${this.RecieveImeiUrl}/CheckErrorsAsync`, { params });
  }

  // Post Receipts
  postReceipts(poId: number, poItemId: number, cmo: string, isReversal: boolean): Observable<ApiResposne> {
    return this.http.post<ApiResposne>(`${this.RecieveImeiUrl}/PostReceiptsAsync`, {
      poId,
      poItemId,
      cmo,
      isReversal
    });
  }

  // Import Scan List
importScanList(items: RecieveIMEIBO[]): Observable<ApiResposne> {
    return this.http.post<ApiResposne>(`${this.RecieveImeiUrl}/InsertScanList`, items);
}
  // Import Packing Slip
 importPackingSlip(items: RecieveIMEIBO[]): Observable<ApiResposne> {
  debugger
  return this.http.post<ApiResposne>(
    `${this.RecieveImeiUrl}/ImportPackingSlip`,
    items
  );
}



// sql


// new IMEI 

getPurchaseOrderss(): Observable<PurchaseOrderListItem[]> {
  return this.http.get<PurchaseOrderListItem[]>(`${this.NewRecieveUrl}/purchase-orders`);
}

  uploadExcel(file: File): Observable<string[]> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<string[]>(`${this.NewRecieveUrl}/upload-excel`, formData);
  }

  checkErrorss(request: CheckErrorsRequest): Observable<CheckErrorsResponse> {
    return this.http.post<CheckErrorsResponse>(`${this.NewRecieveUrl}/check-errors`, request);
  }

  receiveImei(request: ReceiveImeiRequest): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${this.NewRecieveUrl}/receive`, request);
  }


}