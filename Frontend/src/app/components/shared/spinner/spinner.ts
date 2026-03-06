import { Component } from '@angular/core';
import { SpinnerService } from './spinner-service';
import { CommonModule, AsyncPipe } from '@angular/common'; // Dono mein se koi bhi use kar sakte hain

@Component({
  selector: 'app-spinner',
  standalone: true,
  // Yahan imports mein AsyncPipe ya CommonModule add karein
  imports: [CommonModule], 
  templateUrl: './spinner.html',
  styleUrl: './spinner.css',
})
export class Spinner {
  constructor(public spinnerService: SpinnerService) {}
}