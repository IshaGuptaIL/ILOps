import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-inventory-edit-component',
  standalone: true,
  imports: [
    CommonModule, 
    RouterModule
  ],
  templateUrl: './inventory-edit-component.html',
  styleUrls: ['./inventory-edit-component.css']
})
export class InventoryEditComponent {
  // Navigation is now handled by routerLink in the HTML
}
