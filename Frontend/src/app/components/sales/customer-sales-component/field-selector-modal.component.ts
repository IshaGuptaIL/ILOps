import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import Swal from 'sweetalert2';
import { CustomerSalesService } from './customer-sales.service';

@Component({
  selector: 'app-field-selector-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './field-selector-modal.component.html',
  styleUrls: ['./field-selector-modal.component.css']
})
export class FieldSelectorComponent implements OnInit {
  @Input() customerGroup: string = '';
  @Input() fields: any[] = [];
  @Input() onClose: () => void = () => {};

  selectedIndex: number = -1;

  constructor(private customerSalesService: CustomerSalesService) {}

  ngOnInit(): void {
    if (!this.fields || this.fields.length === 0) {
      this.loadFields();
    }
  }

  loadFields() {
    this.customerSalesService.getCustomerFields(this.customerGroup).subscribe((fields:any) => {
      this.fields = fields.sort((a:any, b:any) => a.sequence - b.sequence);
    });
  }

  selectRow(index: number) {
    this.selectedIndex = index;
  }

  moveUp() {
    if (this.selectedIndex > 0) {
      const temp = this.fields[this.selectedIndex];
      this.fields[this.selectedIndex] = this.fields[this.selectedIndex - 1];
      this.fields[this.selectedIndex - 1] = temp;
      
      // Update sequences
      this.updateSequences();
      this.selectedIndex--;
    }
  }

  moveDown() {
    if (this.selectedIndex !== -1 && this.selectedIndex < this.fields.length - 1) {
      const temp = this.fields[this.selectedIndex];
      this.fields[this.selectedIndex] = this.fields[this.selectedIndex + 1];
      this.fields[this.selectedIndex + 1] = temp;
      
      // Update sequences
      this.updateSequences();
      this.selectedIndex++;
    }
  }

  updateSequences() {
    this.fields.forEach((f, i) => f.sequence = i + 1);
  }

  save() {
    this.customerSalesService.updateCustomerFields(this.customerGroup, this.fields).subscribe((res:any) => {
      if (res) {
        Swal.fire('Success', 'Fields updated successfully', 'success');
        this.close();
      }
    });
  }

  close() {
    this.onClose();
  }
}
