import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environments.development';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Router } from '@angular/router';

@Injectable({
  providedIn: 'root',
})
export class Auth {
  
readonly ApiUrl = environment.apiUrl;

constructor(private http:HttpClient,private router :Router) {
  
}


login(login :any):Observable<any>
{
  return this.http.post<any>(this.ApiUrl +"/Login/login",login)
}
}
