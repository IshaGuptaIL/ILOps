import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface CustomerSalesRequest {
  startDate: string;
  endDate: string;
  custGroup?: string;
  msdCode?: string;
  territoryCode?: string;
}

export interface CustomerGroupBO {
  custGroup: string;
  groupName: string;
  bvCustCount: number;
}

export interface BVCustomerBO {
  bvCustNo: string;
  bvName: string;
}

export interface CustomerSalesRow {
  [key: string]: any;
  webOrderID: string;
  invoice: string;
  invoiceDate: string;
  voicePlanDescription: string;
  dataPlanDescription: string;
  cellPhoneNo: string;
  userName: string;
  poNo: string;
  costBudgetCode: string;
  partNumber: string;
  hardwareDescription: string;
  hdwQty: number;
  imeiesn: string;
  accParts: string;
  accessoryDescription: string;
  accQtys: string;
  shipToProvince: string;
  invoiceNet: number;
  invoiceShipping: number;
  invoiceTaxes: number;
  invoiceTotal: number;
  custGroup: string;
  custNO: string;
  typeOfService: string;
  pinNumber: string;
  hstgst: number;
  pstqst: number;
  msdCode: string;
  customerName: string;
  territory: string;
  accountCode: string;
  authorizedDepartment: string;
  shipToAddress: string;
  shipToStreetAddress: string;
  shipToCity: string;
  shipToPostal: string;
  gstRate: number;
  pstRate: number;
  gstFlag: string;
  pstFlag: string;
  tax1Code: number;
  tax2Code: number;
  portedCTN: string;
  bulkOrderID: string;
  hardwareCharge: number;
  accessoryCharge: number;
  arStatus: string;
  userPayAmount: number;
  userPayMethod: string;
  balance: number;
}

@Injectable({
  providedIn: 'root'
})
export class CustomerSalesService {
  private apiUrl = `${environment.apiUrl}/CustomerSales`;

  constructor(private http: HttpClient) { }

  getCustomerGroups(): Observable<CustomerGroupBO[]> {
    return this.http.get<CustomerGroupBO[]>(`${this.apiUrl}/GetCustomerGroups`);
  }

  getCustomersInGroup(groupName: string): Observable<BVCustomerBO[]> {
    return this.http.get<BVCustomerBO[]>(`${this.apiUrl}/GetCustomersInGroup/${groupName}`);
  }

  generateData(request: CustomerSalesRequest): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/GenerateData`, request);
  }

  getGeneratedData(groupName: string): Observable<CustomerSalesRow[]> {
    return this.http.get<CustomerSalesRow[]>(`${this.apiUrl}/GetGeneratedData/${groupName}`);
  }

  exportExcel(request: CustomerSalesRequest): Observable<Blob> {
    return this.http.post(`${this.apiUrl}/ExportExcel`, request, { responseType: 'blob' });
  }

  exportCsv(request: CustomerSalesRequest): Observable<Blob> {
    return this.http.post(`${this.apiUrl}/ExportCsv`, request, { responseType: 'blob' });
  }

  exportPerCustomer(request: CustomerSalesRequest): Observable<Blob> {
    return this.http.post(`${this.apiUrl}/ExportPerCustomer`, request, { responseType: 'blob' });
  }

  generateByMSD(request: CustomerSalesRequest): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/GenerateByMSD`, request);
  }

  generateByTerritory(request: CustomerSalesRequest): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/GenerateByTerritory`, request);
  }

  addFDDealerGroup(): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/AddFDDealerGroup`, {});
  }

  createCustomerGroup(request: { custGroup: string, groupName: string, bvCustNo: string, includeFrench: boolean }): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/CreateGroup`, request);
  }

  deleteCustomerGroup(groupName: string): Observable<boolean> {
    return this.http.delete<boolean>(`${this.apiUrl}/DeleteGroup/${groupName}`);
  }

  getCustomerFields(groupName: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/GetFields/${groupName}`);
  }

  updateCustomerFields(groupName: string, fields: any[]): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/UpdateFields/${groupName}`, fields);
  }

  exportSunLife(request: CustomerSalesRequest): Observable<Blob> {
    return this.http.post(`${this.apiUrl}/ExportSunLife`, request, { responseType: 'blob' });
  }

  exportSplitPayment(request: CustomerSalesRequest, format: string): Observable<Blob> {
    return this.http.post(`${this.apiUrl}/ExportSplitPayment/${format}`, request, { responseType: 'blob' });
  }

  updateGeneratedData(data: CustomerSalesRow[]): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/UpdateGeneratedData`, data);
  }

  addCustomerToGroup(groupCode: string, customer: BVCustomerBO): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/AddCustomerToGroup/${groupCode}`, customer);
  }

  updateCustomerInGroup(groupCode: string, oldCustNo: string, customer: BVCustomerBO): Observable<boolean> {
    return this.http.put<boolean>(`${this.apiUrl}/UpdateCustomerInGroup/${groupCode}/${oldCustNo}`, customer);
  }

  removeCustomerFromGroup(groupCode: string, custNo: string): Observable<boolean> {
    return this.http.delete<boolean>(`${this.apiUrl}/RemoveCustomerFromGroup/${groupCode}/${custNo}`);
  }
}
