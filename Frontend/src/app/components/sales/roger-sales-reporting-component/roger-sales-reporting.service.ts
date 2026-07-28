import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { delay } from 'rxjs/operators';
import { environment } from '../../../../environments/environments.development';

interface SalesActivationRow {
  Invoice10: string;
  TransactionNo: string;
  InvoiceDate: Date;
  OrderDate: Date;
  CustName: string;
  CustTerritory: string;
  UserName: string;
  CellPhoneNo: string;
  VoicePlan: string;
  DataPlan: string;
  WebOrderID: string;
  Type: string;
  AdjustmentType: string;
  Supress: boolean;
  Fee: number;
  FeeCount: number;
  TopUpOwing: number;
  // Department columns
  CoOpAdvertisingHO: number;
  MiscellaneousGBMNDSIncExp: number;
  OtherRevenueHO: number;
  OtherRevenueCO: number;
  ReceivableUpfrontEdgeRV: number;
  SalesAccessoriesCO: number;
  SalesHardwareCO: number;
  StagingAndDeployment: number;
  UnallocatedSales: number;
  WebHosting: number;
  // Additional columns
  PartNumber: string;
  ProductCode: string;
  IMEIESN: string;
  CostPrice: number;
  SellPrice: number;
  InvoiceNet: number;
  InvoiceTotal: number;
}

@Injectable({
  providedIn: 'root'
})
export class RogerSalesReportingService {

  private apiUrl = `${environment.apiUrl}/sales/rogerssalesreporting`;

  constructor(private http: HttpClient) { }

  executeViewAction(endpoint: string, startDate: string, endDate: string, criteria: string, territory: string): Observable<SalesActivationRow[]> {
    let params = new HttpParams()
      .set('startDate', startDate)
      .set('endDate', endDate)
      .set('criteria', criteria);
    
    if (criteria === 'Specific Territory' && territory) {
      params = params.set('territory', territory);
    }

    return this.http.get<SalesActivationRow[]>(`${this.apiUrl}/${endpoint}/view`, { params });
  }

  executeOutputAction(endpoint: string, startDate: string, endDate: string, criteria: string, territory: string): Observable<Blob> {
    let params = new HttpParams()
      .set('startDate', startDate)
      .set('endDate', endDate)
      .set('criteria', criteria);
    
    if (criteria === 'Specific Territory' && territory) {
      params = params.set('territory', territory);
    }

    return this.http.get(`${this.apiUrl}/${endpoint}/output`, { 
      params, 
      responseType: 'blob' 
    });
  }

  exportFilteredData(data: SalesActivationRow[], title: string): Observable<Blob> {
    return this.http.post(`${this.apiUrl}/export-filtered`, { data, title }, { 
      responseType: 'blob' 
    });
  }

  downloadExcel(endpoint: string, startDate: string, endDate: string, criteria: string, territory: string): Observable<Blob> {
    let params = new HttpParams()
      .set('startDate', startDate)
      .set('endDate', endDate)
      .set('criteria', criteria)
      .set('territory', territory);
      
    return this.http.get(`${this.apiUrl}/${endpoint}`, { params, responseType: 'blob' });
  }

  updateRow(row: any): Observable<any> {
    return this.http.put(`${this.apiUrl}/update`, row);
  }

  // Legacy method for backward compatibility
  executeAction(endpoint: string, actionType: 'view' | 'output', startDate: string, endDate: string, criteria: string, territory: string): Observable<any> {
    if (actionType === 'view') {
      return this.executeViewAction(endpoint, startDate, endDate, criteria, territory);
    } else {
      return this.executeOutputAction(endpoint, startDate, endDate, criteria, territory);
    }
  }
}
