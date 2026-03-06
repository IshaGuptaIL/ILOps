import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environments.development';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ApiResponse {
  success: boolean;
  message: string;
  result?: any;
  count?: number;   
  activity?: number;
}

export interface ACCEditResponse {
  items: any[];
  totalItems: number;
}
@Injectable({
  providedIn: 'root',
})
export class AnayseCountService {
  
  private readonly apiUrl = `${environment.apiUrl}/CountAnalysis`;

  constructor(private http: HttpClient) {}


  // 1 ROW
importIMEICounts(file: File): Observable<ApiResponse> {
  const formData = new FormData();
  formData.append('excelFile', file); 

  return this.http.post<ApiResponse>(`${this.apiUrl}/upload-imei`, formData);
}

getAllImportedCounts(): Observable<ApiResponse> {
  return this.http.get<ApiResponse>(`${this.apiUrl}/view-counts`);
}

getOnhandNotCounted(): Observable<ApiResponse> {
  return this.http.get<ApiResponse>(`${this.apiUrl}/onhand-not-counted`);
}
getWarehouseAssignments(page: number, size: number): Observable<ApiResponse> {
  return this.http.get<ApiResponse>(`${this.apiUrl}/warehouse-assignments?pageNumber=${page}&pageSize=${size}`);
}
getDuplicateCounts(page: number, size: number): Observable<ApiResponse> {
  return this.http.get<ApiResponse>(`${this.apiUrl}/duplicate-counts?pageNumber=${page}&pageSize=${size}`);
}
getSystemDuplicates(page: number, size: number): Observable<ApiResponse> {
  return this.http.get<ApiResponse>(`${this.apiUrl}/system-duplicates?pageNumber=${page}&pageSize=${size}`);
}
processDuplicates(): Observable<ApiResponse> {
  return this.http.post<ApiResponse>(`${this.apiUrl}/process-duplicates`, {});
}
getCleanupPreview(): Observable<ApiResponse> {
  return this.http.get<ApiResponse>(`${this.apiUrl}/cleanup-preview`);
}
deleteDuplicates(): Observable<ApiResponse> {
  return this.http.post<ApiResponse>(`${this.apiUrl}/delete-duplicates`, {});
}
getInvalidSerials(): Observable<ApiResponse> {
  return this.http.get<ApiResponse>(`${this.apiUrl}/invalid-serials`);
}
getSystemSerialVerify(): Observable<ApiResponse> {
  return this.http.get<ApiResponse>(`${this.apiUrl}/system-serial-verify`);
}
getDiscrepancyReport(): Observable<ApiResponse> {
  return this.http.get<ApiResponse>(`${this.apiUrl}/discrepancy-report`);
}
getQtyVsSerialComparison(): Observable<ApiResponse> {
  return this.http.get<ApiResponse>(`${this.apiUrl}/qty-vs-serial-comparison`);
}
getMissingFromCount(): Observable<ApiResponse> {
  return this.http.get<ApiResponse>(`${this.apiUrl}/missing-from-count`);
}
processNotOnhandDetails(): Observable<ApiResponse> {
  return this.http.post<ApiResponse>(`${this.apiUrl}/process-not-onhand`, {});
}



  // 3 ROW
  getAccessoryTotals(startDate: string, endDate: string): Observable<ApiResponse> {
    let params = new HttpParams()
      .set('startDate', startDate)
      .set('endDate', endDate);

    return this.http.get<ApiResponse>(`${this.apiUrl}/accessory-totals`, { params });
  }

  getAccessorySalesByChannel(startDate: string, endDate: string): Observable<ApiResponse> {
    let params = new HttpParams().set('startDate', startDate).set('endDate', endDate);
    return this.http.get<ApiResponse>(`${this.apiUrl}/accessory-sales-channel`, { params });
  }
getItemSalesSummary(): Observable<ApiResponse> {
  return this.http.get<ApiResponse>(`${this.apiUrl}/item-sales-summary`);
}

getAccessoryAnalysis(startDate: string, endDate: string): Observable<ApiResponse> {
  let params = new HttpParams()
    .set('startDate', startDate)
    .set('endDate', endDate)
  
  return this.http.get<ApiResponse>(`${this.apiUrl}/accessory-analysis`, { params });
}
getItemReceiptsSummary(startDate: string, endDate: string): Observable<ApiResponse> {
    let params = new HttpParams().set('startDate', startDate).set('endDate', endDate);
    return this.http.get<ApiResponse>(`${this.apiUrl}/item-receipts-summary`, { params });
  }


// 2ROW
getWarehouses(): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/warehouses`);
  }

  getCountFiles(countType: 'hardware' | 'accessory'): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/countFiles?type=${countType}`);
  }

  getCountFileSummary(fileName: string, type: string): Observable<any> {
    const params = new HttpParams()
      .set('fileName', fileName)
      .set('type', type);
    return this.http.get<any>(`${this.apiUrl}/fileSummary`, { params });
  }

assignCountsToWarehouse(request: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/assignCounts`, request);
}

uploadACCCounts(file: File): Observable<ApiResponse> {
    const formData = new FormData();
    formData.append('excelFile', file); 
    return this.http.post<ApiResponse>(`${this.apiUrl}/upload-acc`, formData);
  }
  
  uploadBackOrders(file: File): Observable<ApiResponse> {
  const formData = new FormData();
  formData.append('excelFile', file); 
  return this.http.post<ApiResponse>(`${this.apiUrl}/upload-backorders`, formData);
}

getAccCountsEdit(): Observable<ACCEditResponse> {
    return this.http.get<ACCEditResponse>(`${this.apiUrl}/acc-counts-edit`);
}

  updateAccQty(id: number, newQty: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/update-acc-qty`, { id, newQty });
  }
loadSpireSalesReceipts(type: string): Observable<ApiResponse> {
  let params = new HttpParams().set('type', type); // Yahan 'type' hi hona chahiye
  return this.http.post<ApiResponse>(`${this.apiUrl}/sync-spire-data`, {}, { params });
}
getAccessoryDiscrepancies(): Observable<ApiResponse> {
  return this.http.get<ApiResponse>(
    `${this.apiUrl}/accessory-discrepancies`
  );
}
getCountedNotInBV(): Observable<ApiResponse> {
  return this.http.get<ApiResponse>(`${this.apiUrl}/counted-not-in-bv`);
}
getOnhandNotCounteds(): Observable<ApiResponse> {
    return this.http.get<ApiResponse>(`${this.apiUrl}/onhand-not-counteds`);
}
getLoadedStockStatus(): Observable<ApiResponse> {
    return this.http.get<ApiResponse>(`${this.apiUrl}/loaded-stock-status`);
}

importBackorders(file: File): Observable<ApiResponse> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<ApiResponse>(`${this.apiUrl}/import-backorders`, formData);
}
  // 2ROW
}