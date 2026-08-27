import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface RMAResult {
  id: number;
  sku?: string;
  imei?: string;
  returnReasonCode?: string;
  extraInfo?: string;
  outputCSV?: boolean;
  outputCSVDate?: string | Date;
  outputCSVBatch?: string;
  validationResults?: string;
  rogersResponse?: string;
  invoiceSold?: string;
  invoiceSoldDate?: string | Date;
  whseSold?: string;
  bvCreditOrder?: string;
  returnedRogers?: string;
  returnedRogersBVOrder?: string;
  swap?: string;
  swapCMO?: string;
  pristine?: boolean;
  rejectedACT?: boolean;
  closed?: boolean;
  finalDisposition?: string;
  returnWaybill?: string;
  logInDate?: string | Date;
  creditAmtClaimed?: number;
  user?: string;
  status?: string;
}

export interface RogersResponseItem {
  id: number;
  imei?: string;
  rogersResponse?: string;
  rmaNumber?: string;
  rmaDate?: string | Date;
  headerReturnReason?: string;
  fileName?: string;
  item?: string;
  qty?: number;
  dateReceived?: string | Date;
  dateIssued?: string | Date;
  vpfLastMoveDate?: string | Date;
  vpfAssignDate?: string | Date;
  returnReason?: string;
  creditAmount?: number;
  restockFee?: number;
  totalCredit?: number;
  status?: string;
  lastStatusMessage?: string;
  rmaUpdated?: boolean;
  rejectReason?: string;
  rejectReasonComment?: string;
}

export interface RogersReportCMRMAItem {
  id: number;
  cmNumber?: string;
  cmDate?: string | Date;
  cmAmount?: number;
  rma?: string;
  sku?: string;
  qty?: number;
  unitPrice?: number;
  rmAmount?: number;
  rmAmountTotal?: number;
  imeirma?: string;
  cmImportFile?: string;
  rmImportFile?: string;
}

export interface IMEISearchResponse {
  rmaResults: RMAResult[];
  rogersResponses: RogersResponseItem[];
  cmRmaResults: RogersReportCMRMAItem[];
}

@Injectable({
  providedIn: 'root'
})
export class ImeiSearchService {
  private apiUrl = `${environment.apiUrl}/sales/rmareporting/imeisearch`;

  constructor(private http: HttpClient) {}

  search(criteria: string, query: string): Observable<IMEISearchResponse> {
    let params = new HttpParams()
      .set('criteria', criteria || 'IMEI')
      .set('query', query || '');

    return this.http.get<IMEISearchResponse>(`${this.apiUrl}/search`, { params, withCredentials: true });
  }
}
