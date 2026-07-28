import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../../environments/environment';

export interface ClaimsSummaryRow {
  claimBatchID: number;
  datePriceDrop?: string;
  partNo: string;
  priceBefore: number;
  priceAfter: number;
  count: number;
  totalClaimed: number;
  totalPaid: number;
  minOfID: number;
  totalOutstanding: number;
}

export interface CreditSummaryRow {
  claimBatchID: number;
  creditNoteNumber?: string;
  datePriceDrop?: string;
  partNo: string;
  creditDate?: string;
  maxOfPriceBeforeDrop: number;
  maxOfPriceAfterDrop: number;
  count: number;
  unitAmount: number;
  totalClaimed: number;
  totalPaid: number;
  creditCount: number;
  minOfID: number;
  totalOutstanding: number;
}

export interface UnpaidClaimsDetailRow {
  claimBatchID: number;
  id: number;
  priceDropDate?: string;
  sku: string;
  creditNoteNumber?: string;
  imei: string;
  receiptDate?: string;
  receiptCost: number;
  priceBeforeDrop: number;
  priceAfterDrop: number;
  claimAmount: number;
  claimAmountPaid: number;
  selected?: boolean; // client-side UI flag
}

export interface CreditDetailRow {
  unitCreditAmount: number;
  creditNoteNumber?: string;
  creditNoteDate?: string;
  ppClaimID: number;
  imei: string;
}

export interface ApplyCreditRequest {
  claimBatchID: number;
  creditNoteNumber?: string;
  selectedClaimIds: number[];
  applyCreditNoteNumber: string;
  applyCreditNoteDate: string;
  creditUnitAmount: number;
}

@Injectable({
  providedIn: 'root'
})
export class ApplyCreditReviewClaimsService {
  private apiUrl = `${environment.apiUrl}/ApplyCreditReviewClaims`;

  constructor(private http: HttpClient) { }

  // #region Claims Summary endpoints
  getClaimsSummary(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/claims-summary`);
  }

  exportClaimsSummary(): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/export-claims-summary`, { responseType: 'blob' });
  }
  // #endregion

  // #region Credit Summary endpoints
  getCreditSummary(batchId: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/credit-summary/${batchId}`);
  }
  // #endregion

  // #region Unpaid Claims & Details endpoints
  getUnpaidClaimsDetail(batchId: number, creditNoteNumber?: string): Observable<any> {
    let params = new HttpParams().set('batchId', batchId.toString());
    if (creditNoteNumber) {
      params = params.set('creditNoteNumber', creditNoteNumber);
    }
    return this.http.get<any>(`${this.apiUrl}/unpaid-claims-detail`, { params });
  }

  getCreditDetail(claimId: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/credit-detail/${claimId}`);
  }
  // #endregion

  // #region Modification & Credit application endpoints
  modifyCreditNoteNumber(oldCreditNoteNumber: string, newCreditNoteNumber: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/modify-credit-number`, {
      oldCreditNoteNumber,
      newCreditNoteNumber
    });
  }

  applyCredit(request: ApplyCreditRequest): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/apply-credit`, request);
  }
  // #endregion
}
