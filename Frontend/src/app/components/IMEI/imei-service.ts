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



@Injectable({
  providedIn: 'root',
})
export class ImeiService {
  private ApiUrl = environment.apiUrl;
  
   private readonly RecieveImeiUrl = environment.apiUrl + '/RecieveImei';

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


}
// sql
