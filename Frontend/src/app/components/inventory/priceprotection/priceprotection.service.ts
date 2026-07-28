import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { delay } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';

export interface PriceProtectionBatchRow {
  id: number;
  receiptNo?: string;
  receiptDate?: string;
  receiptCost: number;
  priceDropDate?: string;
  sku?: string;
  description?: string;
  imei?: string;
  claimDate?: string;
  claimAmount: number;
  priceBeforeDrop: number;
  priceAfterDrop: number;
  previousClaim: number;
  memo?: string;
  poNumber?: string;
  claimAmountPaid: number;
}

export interface ReceiptInfoBO {
  partNo?: string;
  cost: number;
  description?: string;
  qty: number;
  poNumber?: string;
}

export interface PostedClaimSummaryBO {
  claimBatchID: number;
  sku?: string;
  description?: string;
  claimDate?: string;
  unitCount: number;
  totalClaimAmount: number;
}

@Injectable({
  providedIn: 'root'
})
export class PriceProtectionService {
  private apiUrl = `${environment.apiUrl}/PriceProtection`;

  constructor(private http: HttpClient) { }

  // #region Onhand Claim API Endpoints
  loadClaimData(sku: string, onhandDate: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/load-claim-data`, { sku, onhandDate });
  }

  processOnhandClaim(sku: string, onhandDate: string, priceBefore: number, priceAfter: number): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/process-onhand-claim`, { sku, onhandDate, priceBefore, priceAfter });
  }
  // #endregion

  // #region Receipt Claim API Endpoints
  findReceipt(receiptNo: string): Observable<any> {
    const params = new HttpParams().set('receiptNo', receiptNo);
    return this.http.get<any>(`${this.apiUrl}/find-receipt`, { params });
  }

  processReceiptClaim(receiptNo: string, dropDate: string, priceBefore: number, priceAfter: number): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/process-receipt-claim`, { receiptNo, dropDate, priceBefore, priceAfter });
  }
  // #endregion

  // #region Manual IMEI Claim API Endpoints
  manualAddImei(imei: string, priceBefore: number, priceAfter: number, onhandDate: string, sku: string, description: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/manual-add-imei`, { imei, priceBefore, priceAfter, onhandDate, sku, description });
  }

  manualRemoveImei(imei: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/manual-remove-imei`, JSON.stringify(imei), {
      headers: { 'Content-Type': 'application/json' }
    });
  }
  // #endregion

  // #region Batch Data & Actions API Endpoints
  getBatchData(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/batch-data`);
  }

  appendClaim(password: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/append-claim`, { password });
  }

  removeBatch(batchNo: number): Observable<any> {
    return this.http.delete<any>(`${this.apiUrl}/remove-batch/${batchNo}`);
  }

  getPostedSummary(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/posted-summary`);
  }

  exportRawData(start: string, end: string): Observable<Blob> {
    const params = new HttpParams().set('start', start).set('end', end);
    return this.http.get(`${this.apiUrl}/export-raw-data`, { params, responseType: 'blob' });
  }

  getNextBatchID(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/next-batch-id`);
  }
  // #endregion

  // #region ImeiSearch (Real API Integration)
  searchImei(imei: string): Observable<any> {
    return this.http.get<any>(`${environment.apiUrl}/ImeiSearch/search/${imei}`);
  }
  // #endregion

  // #region OutputToExcel (Real API Integration)
  exportPriceProtectionBatch(batchId: number): Observable<Blob> {
    return this.http.get(`${environment.apiUrl}/OutputToExcel/export-batch/${batchId}`, { responseType: 'blob' });
  }

  exportRogersOverpayments(): Observable<Blob> {
    return this.http.get(`${environment.apiUrl}/RogerOverPayments/export`, { responseType: 'blob' });
  }

  exportClaimsToCredits(): Observable<Blob> {
    return this.http.get(`${environment.apiUrl}/OutputToExcel/export-claims-to-credits`, { responseType: 'blob' });
  }

  getClaimsToCreditsData(): Observable<any> {
    return this.http.get<any>(`${environment.apiUrl}/OutputToExcel/claims-to-credits-data`);
  }
  // #endregion

  // #region RogerOverPayments (Real API Integration)
  getImportedFilesSummary(): Observable<any> {
    return this.http.get<any>(`${environment.apiUrl}/RogerOverPayments/imported-files`);
  }

  importRogersOverpayments(file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<any>(`${environment.apiUrl}/RogerOverPayments/import`, formData);
  }

  removeRecordsByFile(filename: string): Observable<any> {
    const params = new HttpParams().set('filename', filename);
    return this.http.delete<any>(`${environment.apiUrl}/RogerOverPayments/remove-file`, { params });
  }

  downloadRogersTemplate(): Observable<Blob> {
    return this.http.get(`${environment.apiUrl}/RogerOverPayments/template`, { responseType: 'blob' });
  }
}
