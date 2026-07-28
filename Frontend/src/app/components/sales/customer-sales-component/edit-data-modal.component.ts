import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CustomerSalesRow } from './customer-sales.service';

@Component({
  selector: 'app-edit-data-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './edit-data-modal.component.html',
  styleUrls: ['./edit-data-modal.component.css']
})
export class EditDataModalComponent {
  @Input() data: CustomerSalesRow[] = [];
  @Input() fields: any[] = [];
  @Output() onSave = new EventEmitter<void>();
  @Output() onClose = new EventEmitter<void>();

  save() {
    this.onSave.emit();
    this.close();
  }

  close() {
    this.onClose.emit();
  }

  // ✅ Helper to handle PascalCase (DB) vs camelCase (JSON) mismatch
  getCellData(row: any, fieldName: string): any {
    if (row[fieldName] !== undefined) return row[fieldName];

    // Try camelCase (e.g. Invoice -> invoice)
    const camel = fieldName.charAt(0).toLowerCase() + fieldName.slice(1);
    if (row[camel] !== undefined) return row[camel];

    return '';
  }

  // ✅ Setter to ensure edits go to the right property
  setCellData(row: any, fieldName: string, value: any) {
    if (row[fieldName] !== undefined) {
      row[fieldName] = value;
    } else {
      const camel = fieldName.charAt(0).toLowerCase() + fieldName.slice(1);
      row[camel] = value;
    }
  }
}
