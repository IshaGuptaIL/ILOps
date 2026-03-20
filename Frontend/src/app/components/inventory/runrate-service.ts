import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environments.development';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class RunrateService {
  

    private readonly RunRateUrl = environment.apiUrl + '/RunRate'; 
 constructor(private http: HttpClient) { }

  
  getWFHInventory(): Observable<any> {
    return this.http.get(`${this.RunRateUrl}/GetWfhInventory`);
  }
}
