import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { timeout } from 'rxjs/operators';

export interface TerritoryGroup {
  id: number;
  groupName: string;
  groupCriteria?: string;
  sortOrder?: number;
  phone1?: string;
  phone2?: string;
  rogersReporting: boolean;
  rogersReportingName?: string;
}

export interface ARCustomerRow {
  cust: string;
  custName?: string;
  custGroup?: string;
  groupAndSingle: boolean;
  saleS_TERR?: string;
  postalCode?: string;
  bvaddrtelno1?: string;
  bvaddremail?: string;
  bvcocontact1name?: string;
  bvcocontact1tel1?: string;
  bvcocontact1email?: string;
  bvcocontact2name?: string;
  bvcocontact2tel1?: string;
  bvcocontact2email?: string;
  bvcocontact3name?: string;
  bvcocontact3tel1?: string;
  bvcocontact3email?: string;
  language?: string;
  addressID?: number;
  sendBulk: boolean;
}

export interface ARTransactionRow {
  id: number;
  checked: boolean;
  cust: string;
  folio?: string;
  topItem?: string;
  type?: string;
  tranS_NO: string;
  reF_NO?: string;
  tranDate?: string;
  d_AMOUNT: number;
  c_AMOUNT: number;
  balance: number;
  amount: number;
  daysOld?: number;
  current: number;
  thirtyDays: number;
  sixtyDays: number;
  ninetyDays: number;
  oneTwentyPlusDays: number;
  activationsTerritory?: string;
  msd?: string;
  webOrderID?: string;
  costBudgetCode?: string;
  customerPONo?: string;
  userName?: string;
  cellPhoneNo?: string;
  countGovChannel?: number;
  countGovFee?: number;
  ban?: string;
  firstNoticeDate?: string;
  firstNoticeBalance?: number;
  secondNoticeDate?: string;
  secondNoticeBalance?: number;
  rootCauseID?: number;
  rootCauseDescription?: string;
  opcResolved: boolean;
  opcDescription?: string;
  bulkID?: string;
  ignoreGroup: boolean;
  billToCust?: string;
}

export interface ARCommentEvent {
  id: number;
  eventType: number;
  eventDescription: string;
  custNo?: string;
  custType?: string;
  eventText?: string;
  eventAmount?: number;
  commentKey?: string;
  addDate?: string;
  addUser?: string;
  modDate?: string;
  modUser?: string;
  transNo?: string;
  eventTransID?: number;
}

export interface UpdateARDetailRequest {
  transNo: string;
  ban?: string;
  rootCauseID?: number;
  opcResolved: boolean;
  opcDescription?: string;
  ignoreGroup: boolean;
  billToCust?: string;
}

export interface AddCommentRequest {
  custNo: string;
  custType: string;
  commentText: string;
  checkedTransNos: string[];
  eventType?: number;
}

export interface CreateNoticeRequest {
  noticeType: number;
  custNo: string;
  custName: string;
  language: string;
  amount: number;
  checkedTransNos: string[];
}

export interface ExportInvoiceRequest {
  invoiceRef: string;
  invoiceType: string; // "Normal" or "Bulk"
  custNo: string;
  custName: string;
}

export interface OutputCheckedDocumentsRequest {
  custNo: string;
  chkSendBulk: boolean;
  checkedTransNos: string[];
}

export interface ARCollectionUser {
  id?: number;
  domainUser: string;
  initials?: string;
  defaultChannel?: number;
  channelName?: string;
  createdBy?: number;
  createdDate?: string;
  modifiedBy?: number;
  modifiedDate?: string;
}

export interface ARCustomerGroup {
  id?: number;
  custGroup: string;
  bvCustNo: string;
  groupName: string;
  bvName: string;
  createdBy?: number;
  createdDate?: string;
  modifiedBy?: number;
  modifiedDate?: string;
}

export interface ARBulkCustomer {
  id?: number;
  custNo: string;
  createdBy?: number;
  createdDate?: string;
  modifiedBy?: number;
  modifiedDate?: string;
}

export interface ARGroupSummary {
  custGroup: string;
  maxOfGroupName: string;
  countOfCustGroup: number;
}

export interface ARGroupCustomerRow {
  id: number;
  custGroup: string;
  bvCustNo: string;
  groupName: string;
  bvName: string;
}

export interface ARBulkCustomerWithName {
  id: number;
  custNo: string;
  name: string;
}

export interface GLAllowedAccount {
  account: string;
  name: string;
}

export interface GLActivityRow {
  accountNo: string;
  accountName: string;
  date?: string;
  transNo: string;
  source: string;
  user: string;
  glMemo?: string;
  type?: string;
  entity?: string;
  document?: string;
  debitAmt: number;
  creditAmt: number;
  balance: number;
  webOrderID?: string;
  postDate?: string;
}

export interface CommentReviewSummaryRow {
  groupID: string;
  maxOfSALES_TERR: string;
  customerName: string;
  arType: string;
  transCount: number;
  sumOfInvoiceCount: number;
  sumOfPaymentCount: number;
  sumOfFirstNoticeCount: number;
  sumOfSecondNoticeCount: number;
  sumOfBALANCE: number;
  bulkInvoice: boolean;
}

export interface BatchNoticeSummaryRow {
  groupID: string;
  maxOfSALES_TERR: string;
  customerName: string;
  arType: string;
  transCount: number;
  sumOfInvoiceCount: number;
  sumOfPaymentCount: number;
  sumOfFirstNoticeCount: number;
  sumOfSecondNoticeCount: number;
  sumOfBALANCE: number;
  bulkInvoice: boolean;
}

export interface BatchNoticeDetailRow {
  cust: string;
  groupID: string;
  saleS_TERR: string;
  custType: string;
  custName: string;
  custGroup: string;
  folio: string;
  topItem: string;
  type: string;
  tranS_NO: string;
  reF_NO: string;
  tranDate?: string;
  d_AMOUNT: number;
  c_AMOUNT: number;
  balance: number;
  daysOld: number;
  checked: boolean;
  firstNoticeDate?: string;
  firstNoticeBalance?: number;
  secondNoticeDate?: string;
  secondNoticeBalance?: number;
  invoiceCount: number;
  paymentCount: number;
  firstNoticeCount: number;
  secondNoticeCount: number;
  bulkID: string;
  bulkIDChecked: boolean;
  language: string;
  sendBulk: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class ArCollectionService {
  private apiUrl = `${environment.apiUrl}/ARCollections`;

  constructor(private http: HttpClient) { }

  getTerritoryGroups(): Observable<TerritoryGroup[]> {
    return this.http.get<TerritoryGroup[]>(`${this.apiUrl}/TerritoryGroups`);
  }

  loadOpenCustomers(selectBy: number, groupCriteria: string, agingDate: string): Observable<ARCustomerRow[]> {
    const params = new HttpParams()
      .set('selectBy', selectBy.toString())
      .set('groupCriteria', groupCriteria)
      .set('agingDate', agingDate);
    return this.http.get<ARCustomerRow[]>(`${this.apiUrl}/Customers`, { params });
  }

  refreshARGrid(custNo: string, selectBy: number, groupCriteria: string, agingDate: string): Observable<ARTransactionRow[]> {
    const params = new HttpParams()
      .set('custNo', custNo)
      .set('selectBy', selectBy.toString())
      .set('groupCriteria', groupCriteria)
      .set('agingDate', agingDate);
    return this.http.get<ARTransactionRow[]>(`${this.apiUrl}/RefreshARGrid`, { params });
  }

  updateARDetailRow(request: UpdateARDetailRequest): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/UpdateARDetailRow`, request);
  }

  getEvents(custNo: string, selectBy: number): Observable<ARCommentEvent[]> {
    const params = new HttpParams()
      .set('custNo', custNo)
      .set('selectBy', selectBy.toString());
    return this.http.get<ARCommentEvent[]>(`${this.apiUrl}/Events`, { params });
  }

  addComment(request: AddCommentRequest): Observable<number> {
    return this.http.post<number>(`${this.apiUrl}/AddComment`, request);
  }

  deleteComment(commentId: number): Observable<boolean> {
    return this.http.delete<boolean>(`${this.apiUrl}/DeleteComment/${commentId}`);
  }

  editComment(commentId: number, text: string): Observable<boolean> {
    return this.http.put<boolean>(`${this.apiUrl}/EditComment/${commentId}`, JSON.stringify(text), {
      headers: { 'Content-Type': 'application/json' }
    });
  }

  removeCommentFromTrans(eventTransId: number): Observable<boolean> {
    return this.http.delete<boolean>(`${this.apiUrl}/RemoveCommentFromTrans/${eventTransId}`);
  }

  checkOpenPayments(custNo: string): Observable<boolean> {
    const params = new HttpParams().set('custNo', custNo);
    return this.http.get<boolean>(`${this.apiUrl}/CheckOpenPayments`, { params });
  }

  generateOverdueNotice(request: CreateNoticeRequest): Observable<Blob> {
    return this.http.post(`${this.apiUrl}/GenerateOverdueNotice`, request, { responseType: 'blob' });
  }

  outputInvoicePdf(request: ExportInvoiceRequest): Observable<Blob> {
    return this.http.post(`${this.apiUrl}/OutputInvoicePdf`, request, { responseType: 'blob' });
  }

  outputCheckedDocuments(request: OutputCheckedDocumentsRequest): Observable<Blob> {
    return this.http.post(`${this.apiUrl}/OutputCheckedDocuments`, request, { responseType: 'blob' });
  }

  outputPaymentAdvicePdf(transNo: string): Observable<Blob> {
    const params = new HttpParams().set('transNo', transNo);
    return this.http.get(`${this.apiUrl}/OutputPaymentAdvicePdf`, { params, responseType: 'blob' });
  }

  getARUsers(page: number, pageSize: number): Observable<{ data: ARCollectionUser[], total: number }> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    return this.http.get<{ data: ARCollectionUser[], total: number }>(`${this.apiUrl}/Users`, { params });
  }

  createARUser(user: ARCollectionUser): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/CreateUser`, user);
  }

  updateARUser(user: ARCollectionUser): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/UpdateUser`, user);
  }

  deleteARUser(id: number): Observable<boolean> {
    return this.http.delete<boolean>(`${this.apiUrl}/DeleteUser/${id}`);
  }

  // --- Customer Groups Service API ---
  getCustomerGroups(page: number, pageSize: number): Observable<{ data: ARCustomerGroup[], total: number }> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    return this.http.get<{ data: ARCustomerGroup[], total: number }>(`${this.apiUrl}/CustomerGroups`, { params });
  }

  createCustomerGroup(group: ARCustomerGroup): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/CreateCustomerGroup`, group);
  }

  updateCustomerGroup(group: ARCustomerGroup): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/UpdateCustomerGroup`, group);
  }

  deleteCustomerGroup(id: number): Observable<boolean> {
    return this.http.delete<boolean>(`${this.apiUrl}/DeleteCustomerGroup/${id}`);
  }

  // --- Bulk Customers Service API ---
  getBulkCustomers(page: number, pageSize: number): Observable<{ data: ARBulkCustomer[], total: number }> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    return this.http.get<{ data: ARBulkCustomer[], total: number }>(`${this.apiUrl}/BulkCustomers`, { params });
  }

  createBulkCustomer(bulk: ARBulkCustomer): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/CreateBulkCustomer`, bulk);
  }

  deleteBulkCustomer(id: number): Observable<boolean> {
    return this.http.delete<boolean>(`${this.apiUrl}/DeleteBulkCustomer/${id}`);
  }

  // --- Parity with Access Form frmCustGroupMaintain ---
  getGroupsSummary(groupType: string): Observable<ARGroupSummary[]> {
    const params = new HttpParams().set('groupType', groupType);
    return this.http.get<ARGroupSummary[]>(`${this.apiUrl}/GroupsSummary`, { params });
  }

  getGroupCustomers(groupType: string, custGroup: string): Observable<ARGroupCustomerRow[]> {
    const params = new HttpParams()
      .set('groupType', groupType)
      .set('custGroup', custGroup);
    return this.http.get<ARGroupCustomerRow[]>(`${this.apiUrl}/GroupCustomers`, { params });
  }

  lookupCustomerName(custNo: string): Observable<{ exists: boolean, name: string }> {
    const params = new HttpParams().set('custNo', custNo);
    return this.http.get<{ exists: boolean, name: string }>(`${this.apiUrl}/LookupCustomerName`, { params });
  }

  addCustomerToGroup(payload: { groupType: string, custNo: string, isNewGroup: boolean, newGroupName: string, selectedCustGroup: string }): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/AddCustomerToGroup`, payload);
  }

  removeCustomerFromGroup(payload: { groupType: string, custNo: string }): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/RemoveCustomerFromGroup`, payload);
  }

  modifyGroupName(payload: { groupType: string, custGroup: string, newGroupName: string }): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/ModifyGroupName`, payload);
  }

  getBulkCustomersWithName(): Observable<ARBulkCustomerWithName[]> {
    return this.http.get<ARBulkCustomerWithName[]>(`${this.apiUrl}/BulkCustomersWithName`);
  }

  addBulkCustomer(custNo: string): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/AddBulkCustomer`, JSON.stringify(custNo), {
      headers: { 'Content-Type': 'application/json' }
    });
  }

  removeBulkCustomer(id: number): Observable<boolean> {
    return this.http.delete<boolean>(`${this.apiUrl}/RemoveBulkCustomer/${id}`);
  }

  getGLAllowedAccounts(): Observable<GLAllowedAccount[]> {
    return this.http.get<GLAllowedAccount[]>(`${this.apiUrl}/GLAllowedAccounts`);
  }

  getGLActivity(accountNo: string, startDate: string, endDate: string): Observable<GLActivityRow[]> {
    const params = new HttpParams()
      .set('accountNo', accountNo)
      .set('startDate', startDate)
      .set('endDate', endDate);
    return this.http.get<GLActivityRow[]>(`${this.apiUrl}/GLActivity`, { params });
  }

  exportGLActivity(accountNo: string, startDate: string, endDate: string): Observable<Blob> {
    const params = new HttpParams()
      .set('accountNo', accountNo)
      .set('startDate', startDate)
      .set('endDate', endDate);
    return this.http.get(`${this.apiUrl}/ExportGLActivity`, { params, responseType: 'blob' });
  }

  // --- Comment Review Features ---
  generateCommentReviewData(agingDate: string): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/GenerateCommentReviewData`, JSON.stringify(agingDate), {
      headers: { 'Content-Type': 'application/json' }
    });
  }

  getCommentReviewSummary(minDays: number, groupCriteria: string): Observable<CommentReviewSummaryRow[]> {
    const params = new HttpParams()
      .set('minDays', minDays.toString())
      .set('groupCriteria', groupCriteria);
    return this.http.get<CommentReviewSummaryRow[]>(`${this.apiUrl}/CommentReviewSummary`, { params });
  }

  getSummaryComment(custNo: string): Observable<ARCommentEvent> {
    const params = new HttpParams().set('custNo', custNo);
    return this.http.get<ARCommentEvent>(`${this.apiUrl}/SummaryComment`, { params });
  }

  saveSummaryComment(custNo: string, custType: string, commentText: string): Observable<boolean> {
    const payload = { custNo, custType, commentText };
    return this.http.post<boolean>(`${this.apiUrl}/SummaryComment`, payload);
  }

  exportSummaryComments(minDays: number, groupCriteria: string): Observable<Blob> {
    const params = new HttpParams()
      .set('minDays', minDays.toString())
      .set('groupCriteria', groupCriteria);
    return this.http.get(`${this.apiUrl}/ExportSummaryComments`, { params, responseType: 'blob' });
  }

  // #region Reporting component
  generateAgingData(data: { lastReportDate: string, startDate: string, endDate: string }): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/GenerateAgingData`, data).pipe(timeout(600000));
  }

  getAgedSummaryData(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/GetAgedSummaryData`, { withCredentials: true }).pipe(timeout(600000));
  }

  getPaymentDetailsData(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/GetPaymentDetailsData`, { withCredentials: true }).pipe(timeout(600000));
  }

  getARMasterDataGrid(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/reporting/ar-master-data-grid`);
  }

  // --- Batch Notice Output API ---
  generateBatchNoticeData(agingDate: string): Observable<boolean> {
    const params = new HttpParams().set('agingDate', agingDate);
    return this.http.post<boolean>(`${this.apiUrl}/batch-notice/generate`, null, { params });
  }

  getBatchNoticeSummary(groupCriteria: string, startDays: number, endDays: number, noticeType: string): Observable<BatchNoticeSummaryRow[]> {
    const params = new HttpParams()
      .set('groupCriteria', groupCriteria)
      .set('startDays', startDays.toString())
      .set('endDays', endDays.toString())
      .set('noticeType', noticeType);
    return this.http.get<BatchNoticeSummaryRow[]>(`${this.apiUrl}/batch-notice/summary`, { params });
  }

  getBatchNoticeDetail(groupCriteria: string, startDays: number, endDays: number, noticeType: string): Observable<BatchNoticeDetailRow[]> {
    const params = new HttpParams()
      .set('groupCriteria', groupCriteria)
      .set('startDays', startDays.toString())
      .set('endDays', endDays.toString())
      .set('noticeType', noticeType);
    return this.http.get<BatchNoticeDetailRow[]>(`${this.apiUrl}/batch-notice/detail`, { params });
  }

  outputBatchNotices(selectedGroups: string[], noticeType: string, startDays: number, endDays: number, groupCriteria: string): Observable<Blob> {
    const body = { selectedGroups, noticeType, startDays, endDays, groupCriteria };
    return this.http.post(`${this.apiUrl}/batch-notice/output`, body, { responseType: 'blob' });
  }

  getARMasterData(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/GetARMasterData`, { withCredentials: true }).pipe(timeout(600000));
  }

  exportAgedSummary(): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/ExportAgedSummary`, { responseType: 'blob', withCredentials: true }).pipe(timeout(600000));
  }

  generateARMasterData(agingDate: string): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/GenerateARMasterData`, JSON.stringify(agingDate), {
      headers: { 'Content-Type': 'application/json' }
    }).pipe(timeout(600000));
  }

  exportARMaster(): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/ExportARMaster`, { responseType: 'blob' }).pipe(timeout(600000));
  }

  exportARMasterAll(): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/ExportARMasterAll`, { responseType: 'blob' }).pipe(timeout(600000));
  }

  exportARMasterSummary(): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/ExportARMasterSummary`, { responseType: 'blob' }).pipe(timeout(600000));
  }
  // #endregion
}
