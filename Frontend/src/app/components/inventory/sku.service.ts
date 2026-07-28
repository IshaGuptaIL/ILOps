import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { timeout } from 'rxjs/operators';
import { CookieService } from 'ngx-cookie-service';
import { environment } from '../../../environments/environments.development';

/** Minimum API execution timeout: 10 minutes (600 000 ms) */
const API_TIMEOUT_MS = 600_000;

export interface SkuVM {
  id?: number;
  sku: string;
  type: string;
  createdBy?: number;
  createdDate?: string | Date;
  modifiedBy?: number;
  modifiedDate?: string | Date;
}

@Injectable({
  providedIn: 'root',
})
export class SkuService {
  private readonly apiUrl = `${environment.apiUrl}/Sku`;

  constructor(private http: HttpClient, private cookieService: CookieService) {}

  private getUserId(): number {
    const userIdStr =
      this.cookieService.get('userid') ||
      this.cookieService.get('userId') ||
      this.cookieService.get('UserId');
    return userIdStr ? parseInt(userIdStr, 10) : 1;
  }

  getSkus(): Observable<SkuVM[]> {
    return this.http
      .get<SkuVM[]>(this.apiUrl)
      .pipe(timeout(API_TIMEOUT_MS));
  }

  addSku(sku: SkuVM): Observable<boolean> {
    sku.createdBy = this.getUserId();
    return this.http
      .post<boolean>(this.apiUrl, sku)
      .pipe(timeout(API_TIMEOUT_MS));
  }

  updateSku(sku: SkuVM): Observable<boolean> {
    sku.modifiedBy = this.getUserId();
    return this.http
      .put<boolean>(this.apiUrl, sku)
      .pipe(timeout(API_TIMEOUT_MS));
  }

  deleteSku(sku: string): Observable<boolean> {
    return this.http
      .delete<boolean>(`${this.apiUrl}/${sku}`)
      .pipe(timeout(API_TIMEOUT_MS));
  }
}
