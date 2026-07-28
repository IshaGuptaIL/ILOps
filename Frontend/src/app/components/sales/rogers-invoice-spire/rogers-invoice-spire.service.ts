import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface ProcessDataRequest {
  startDate: string;
  endDate: string;
}

export interface ProcessDataResult {
  success: boolean;
  message: string;
}

export interface CostVerificationRow {
  transactionNo?: string;
  invoice?: string;
  invoiceDate?: string;
  custName?: string;
  custTerritory?: string;
  whse?: string;
  partNumber?: string;
  freeAccessory?: string;
  qty?: number;
  imeiesn?: string;
  costPrice?: number;
  sellPrice?: number;
  topUpOwing?: number;
  bvReceiptCost?: number;
  netIMEIReceiveCost?: number;
  netPriceProtection?: number;
  poNumber?: string;
  bvReceipt?: string;
  misC_1?: string;
}

export interface DailySalesRow {
  invoiceNo?: string;
  webOrderID?: string;
  date?: string;
  paymentMethod?: string;
  transNo?: string;
  custNo?: string;
  custName?: string;
  total?: number;
  invTerr?: string;
  custTerr?: string;
}

export interface ReturnsVerificationRow {
  id: number;
  userId: number;
  channelName?: string;
  paymentMethod?: string;
  type?: string;
  invoice?: string;
  invoiceDate?: string;
  custTerritory?: string;
  cellPhoneNo?: string;
  webOrderID?: string;
  qty?: number;
  partNumber?: string;
  freeAccessory?: string;
  imeiesn?: string;
  costPrice?: number;
  sellPrice?: number;
  topUpOwing?: number;
  accessoryCost?: number;
  accessoryPrice?: number;
  topUpAcc?: number;
  topUpTotal?: number;
  arAmount?: number;
  hdwChargeToCustomer?: number;
  trueHDWTopUp?: number;
  accChargeToCx?: number;
  accMargin?: number;
  group?: string;
  source?: string;
  
  channelName2?: string;
  paymentMethod2?: string;
  type2?: string;
  invoice2?: string;
  invoiceDate2?: string;
  custTerritory2?: string;
  cellPhoneNo2?: string;
  webOrderID2?: string;
  qty2?: number;
  partNumber2?: string;
  freeAccessory2?: string;
  imeiesn2?: string;
  costPrice2?: number;
  sellPrice2?: number;
  topUpOwing2?: number;
  accessoryCost2?: number;
  accessoryPrice2?: number;
  topUpAcc2?: number;
  topUpTotal2?: number;
  arAmount2?: number;
  hdwChargeToCustomer2?: number;
  trueHDWTopUp2?: number;
  accChargeToCx2?: number;
  accMargin2?: number;
  group2?: string;
}

@Injectable({
  providedIn: 'root'
})
export class RogersInvoiceSpireService {
  private apiUrl = `${environment.apiUrl}/Sales/RogersInvoiceSpire`;

  constructor(private http: HttpClient) { }

  processData(request: ProcessDataRequest, userId?: number): Observable<ProcessDataResult> {
    const url = userId ? `${this.apiUrl}/ProcessData?userId=${userId}` : `${this.apiUrl}/ProcessData`;
    return this.http.post<ProcessDataResult>(url, request);
  }

  getCostVerificationReport(startDate: string, endDate: string): Observable<CostVerificationRow[]> {
    return this.http.get<CostVerificationRow[]>(`${this.apiUrl}/CostVerificationReport`, {
      params: { startDate, endDate }
    });
  }

  getDailySalesSummary(startDate: string, endDate: string): Observable<DailySalesRow[]> {
    return this.http.get<DailySalesRow[]>(`${this.apiUrl}/DailySalesSummary`, {
      params: { startDate, endDate }
    });
  }

  getReturnsVerificationReport(
    startDate: string, 
    endDate: string, 
    returnsStart: string, 
    returnsEnd: string, 
    userId?: number
  ): Observable<ReturnsVerificationRow[]> {
    const params: any = { startDate, endDate, returnsStart, returnsEnd };
    if (userId) params.userId = userId.toString();
    return this.http.get<ReturnsVerificationRow[]>(`${this.apiUrl}/ReturnsVerificationReport`, { params });
  }

  getHdwFeeReport(userId?: number): Observable<CostVerificationRow[]> {
    let params = new HttpParams();
    if (userId !== undefined && userId !== null) params = params.set('userId', userId.toString());
    return this.http.get<CostVerificationRow[]>(`${this.apiUrl}/HdwFeeCheck`, { params });
  }

  downloadRogersEstimate(userId?: number): Observable<Blob> {
    const url = userId ? `${this.apiUrl}/DownloadRogersEstimate?userId=${userId}` : `${this.apiUrl}/DownloadRogersEstimate`;
    return this.http.get(url, { responseType: 'blob' });
  }
}
