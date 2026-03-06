import { Injectable } from '@angular/core';
import { environment } from '../../../../environments/environments.development';
import { Observable } from 'rxjs';
import { HttpClient, HttpParams } from '@angular/common/http';
export interface ApiResponse<T = any> {
  success: boolean;
  message: string;
  result?: T;
  count?: number;
  statusCode?: number;
}


@Injectable({
  providedIn: 'root',
})
export class InventoryService {

   private readonly ApiUrl = environment.apiUrl + '/AddInventory';
   private readonly ModifyUrl = environment.apiUrl + '/ModifyInventory';
    private CostValidationUrl = environment.apiUrl + '/cost-validation/';
   private readonly InventoryTypeUrl = environment.apiUrl + '/InventoryType';

  constructor(private http: HttpClient) { }

  // =============================
  // Check if part number exists
  // =============================
  checkPartNo(partNo: string, whse: string): Observable<any> {
    return this.http.get(
      `${this.ApiUrl}/CheckPartNo`,
      { params: { partNo, whse } }
    );
  }

  // =============================
  // Add a new inventory item
  // =============================
  addInventoryItem(item: any): Observable<any> {
    return this.http.post(`${this.ApiUrl}/InventoryAdd`, item);
  }

 getWarehouses(userRoleId: number): Observable<any[]> {
  let params = new HttpParams().set('userRoleId', userRoleId);
  
  return this.http.get<any[]>(`${this.ApiUrl}/GetWarehousesAsync`, { params });
}

  // =============================
  // Manufacturers
  // =============================
  getManufacturers(): Observable<any[]> {
    return this.http.get<any[]>(
      `${this.ApiUrl}/GetManufacturersAsync`
    );
  }



// =====================================================
  // =============== MODIFY INVENTORY ====================
  // =====================================================

  // 🔹 Inventory list with pagination + search
  getModifyInventoryList(
    search: string,
    page: number,
    size: number
  ): Observable<any> {

    let params = new HttpParams()
      .set('search', search || '')
      .set('page', page)
      .set('size', size);

    return this.http.get<any>(
      `${this.ModifyUrl}/list`,
      { params }
    );
  }

  // 🔹 Get all warehouses for part number
  getAllWarehousesForPart(
    partNo: string,
    skipWhse: string
  ): Observable<any[]> {

    let params = new HttpParams()
      .set('partNo', partNo)
      .set('skipWhse', skipWhse || '');

    return this.http.get<any[]>(
      `${this.ModifyUrl}/warehouses`,
      { params }
    );
  }

  // 🔹 Update price (single / apply-to-all)
updateInventoryPrice(payload: any, applyToAll: boolean) {
  return this.http.post<any>(
    `${this.ModifyUrl}/update-price`,
    payload,
    {
      params: {
        applyToAll: applyToAll.toString()
      }
    }
  );
}



// =====================================================
  // =============== COST VALIDATIONS ====================
  // =====================================================

getLatestHpc() {
    return this.http.get<any[]>(`${this.CostValidationUrl}/hpc/latest`);
  }

  getHpcDiscrepancy() {
    return this.http.get<any[]>(`${this.CostValidationUrl}/hpc/discrepancy`);
  }

uploadHpc(file: File): Observable<ApiResponse<{
  validRows: any[];
  invalidRows: any[];
  insertedCount: number;
  failedCount: number;
}>> {
  const formData = new FormData();
  formData.append('excelFile', file);
  
  return this.http.post<ApiResponse<{
    validRows: any[];
    invalidRows: any[];
    insertedCount: number;
    failedCount: number;
  }>>('http://localhost:5008/api/cost-validation/upload-hpc', formData);
}


downloadHPCTemplate(): Observable<Blob> {
  return this.http.get(`${this.CostValidationUrl}/download-hpc-template`, {
    responseType: 'blob'  // Important for binary download
  });
}
  export(viewType: string) {
    window.open(`${this.CostValidationUrl}/export?viewType=${viewType}`, '_blank');
  }



 RDHardwareVsSpire(): Observable<any> {
  return this.http.get(`${this.CostValidationUrl}RDHardwareVsSpire`);
}

CostVarianceCurrentVsAvg(): Observable<any> {
  return this.http.get(`${this.CostValidationUrl}CostVarianceCurrentVsAvg`);
}



CostVarianceAcrossWarehouses(): Observable<any> {
  return this.http.get(
    `${this.CostValidationUrl}CostVarianceAcrossWarehouses`
  );
}
HpcDiscrepancies(): Observable<any> {
  return this.http.get(
    `${this.CostValidationUrl}HpcDiscrepancies`
  );

}

HpcLatest(): Observable<any> {
  return this.http.get(
    `${this.CostValidationUrl}HpcLatest`
  );
}


// =====================================================
// =============== INVENTORY TYPES (tblMan) ============
// =====================================================


getFilteredGroups(type: string): Observable<any> {
  return this.http.get(`${this.InventoryTypeUrl}/GetGroups`, {
    params: { type: type || 'HCC' }
  });
}


getInventoryTypes(entryType: string, page: number, pageSize: number): Observable<any> {
  let params = new HttpParams()
    .set('entryType', entryType)
    .set('page', page.toString())
    .set('pageSize', pageSize.toString());

  return this.http.get(`${this.InventoryTypeUrl}/GetData`, { params });
}


addGroup(payload: any): Observable<any> {
  // Payload: { name: '...', inventoryType: 'HCC' }
  return this.http.post(`${this.InventoryTypeUrl}/Add`, payload);
}


updateGroup(payload: any): Observable<any> {
  // Payload: { id: 1, name: '...' }
  return this.http.patch(`${this.InventoryTypeUrl}/Update`, payload);
}

}