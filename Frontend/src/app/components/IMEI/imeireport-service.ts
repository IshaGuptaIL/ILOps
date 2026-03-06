import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environments.development';
import { HttpClient, HttpParams } from '@angular/common/http';
import * as XLSX from 'xlsx';
import { Observable } from 'rxjs';
import saveAs from 'file-saver';


export interface HardwareReceipt {
  vendor: string;
  bvReceiptNo: string;
  bvReceiptDate: string;
  cmo: string;
  po: string;
  part: string;
  qty: number;
  receiptUnitCost: number;
  imei: string;
  rogersTotal?: number;
  rogersCount?: number;
  firstOfTransType: string;
  firstOfRefNo: string;
  firstOfTransDate?: string;
  firstOfPerUnitAmount?: number;
  firstOfRemarks: string;
}


export interface ReceivedReportBO {
  vendor: string;
  bvReceiptNo: string;
  bvReceiptDate: string; // you can also use Date if you parse it later
  cmo: string;
  po: string;
  part: string;
  receiptUnitCost: number;
  imei: string;
  qty: number;
}



@Injectable({
  providedIn: 'root',
})
export class IMEIReportService {
  private readonly baseUrl = environment.apiUrl +'/Reports'


   constructor(private http: HttpClient) {}

    getSpireStockStatus(): Observable<any[]> {
      debugger
    return this.http.get<any[]>(`${this.baseUrl}/stock-status`);
  }

  getVendors() {
    debugger
    return this.http.get<any[]>(`${this.baseUrl}/vendors`);
  }

 getParts(itemType: string) {
  return this.http.get<any>(`${this.baseUrl}/parts/${itemType}`);
}
  getIMEIReport(payload: any) {
    return this.http.post<any[]>(`${this.baseUrl}/imei-report`, payload);
  }

  

    getReceipts(startDate: string, endDate: string, whse: string = 'CO'): Observable<any[]> {debugger
    let params = new HttpParams()
      .set('startDate', startDate)
      .set('endDate', endDate)
      .set('whse', whse);

    return this.http.get<any[]>(`${this.baseUrl}/GetReceipts`, { params });
  }

getHardwareReceipts(params: { receiptNo?: string; poNumber?: string }): Observable<HardwareReceipt[]> {
  
  debugger
  return this.http.get<HardwareReceipt[]>(`${this.baseUrl}/receipts`, { params });
  
}



   exportToExcel(data: any[], fileName: string) {
    if (!data || !data.length) return;

    // 1. Create worksheet
    const ws: XLSX.WorkSheet = XLSX.utils.json_to_sheet(data);

    // 2. Create workbook and add worksheet
    const wb: XLSX.WorkBook = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Sheet1');

    // 3. Write workbook and save
    const excelBuffer: any = XLSX.write(wb, { bookType: 'xlsx', type: 'array' });
    const blob: Blob = new Blob([excelBuffer], { type: 'application/octet-stream' });
    saveAs(blob, `${fileName}.xlsx`);
  }



getReceivedReport(
  itemType: string,
  vendor?: string,
  part?: string,
  startDate?: string,
  endDate?: string
): Observable<ReceivedReportBO[]> {
  const params: any = { itemType };
  if (vendor) params.vendor = vendor;
  if (part) params.part = part;
  if (startDate) params.startDate = startDate;
  if (endDate) params.endDate = endDate;

  // Expect JSON instead of Blob
  return this.http.get<ReceivedReportBO[]>(`${this.baseUrl}/received-report`, { params });
}
}





