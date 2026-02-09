import { Injectable } from '@angular/core';
import { environment } from '../../../../environments/environments.development';
import { Observable } from 'rxjs';
import { HttpClient, HttpParams } from '@angular/common/http';

@Injectable({
  providedIn: 'root',
})
export class InventoryService {

   private readonly ApiUrl = environment.apiUrl + '/AddInventory';
   private readonly ModifyUrl = environment.apiUrl + '/ModifyInventory';
    private CostValidationUrl = environment.apiUrl + '/cost-validation';

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

  uploadHpc(file: File) {
    const fd = new FormData();
    fd.append('excelFile', file);
    return this.http.post<any>(`${this.CostValidationUrl}/upload-hpc`, fd);
  }


downloadHPCTemplate(): Observable<Blob> {
  return this.http.get(`${this.CostValidationUrl}/download-hpc-template`, {
    responseType: 'blob'  // Important for binary download
  });
}
  export(viewType: string) {
    window.open(`${this.CostValidationUrl}/export?viewType=${viewType}`, '_blank');
  }


uploadHPC(file: File): Observable<any> {
  const formData = new FormData();
  formData.append('excelFile', file, file.name);

  // ✅ Yeh exact match karo
  return this.http.post<any>('api/CostValidation/load-hpc', formData);
}



}



