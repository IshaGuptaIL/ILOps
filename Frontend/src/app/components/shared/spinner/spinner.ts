import { Component } from '@angular/core';
import { SpinnerService } from './spinner-service';

@Component({
  selector: 'app-spinner',
  imports: [],
  templateUrl: './spinner.html',
  styleUrl: './spinner.css',
})
export class Spinner {
 
  constructor(public spinnerService:SpinnerService) {
    
  }
  get isActive() {
    return this.spinnerService.isActive;
  }

}
