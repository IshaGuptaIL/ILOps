import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RogersReportImportService } from './rogers-report-import.service';

@Component({
  selector: 'app-rogers-report-import',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './rogers-report-import.component.html',
  styleUrls: ['./rogers-report-import.component.css']
})
export class RogersReportImportComponent {
  
  constructor(private importService: RogersReportImportService) {}

  onDownloadTemplate(fileType: string) {
    console.log('Downloading template for:', fileType);
    this.importService.downloadTemplate(fileType);
  }

  onFileUpload(event: any, fileType: string) {
    const file = event.target.files[0];
    if (file) {
      console.log(`Uploading ${fileType} file:`, file.name);
      this.importService.uploadFile(file, fileType).subscribe({
        next: (res) => alert(res.message || 'File processed successfully'),
        error: (err) => alert('Error processing file: ' + (err.error || err.message))
      });
    }
  }

  onTriggerUploadClick(inputId: string) {
    const fileInput = document.getElementById(inputId) as HTMLInputElement;
    if (fileInput) {
      fileInput.click();
    }
  }

  onButtonClick(action: string) {
    console.log('Button clicked:', action);
    if (action === 'CM Summary') {
      this.importService.generateCmSummary().subscribe({
        next: (res) => alert(res.message),
        error: (err) => alert('Error: ' + err.message)
      });
    } else if (action === 'Manual RMA Import' || action === 'Import Manual') {
      this.importService.processManualImport().subscribe({
        next: (res) => alert(res.message),
        error: (err) => alert('Error: ' + err.message)
      });
    } else if (action === 'Delete Files') {
      // For demo, sending dummy strings. Normally these would be bound to the select dropdowns.
      this.importService.deleteBatchFiles('cm.xlsx', 'rm.xlsx', 'manual.xlsx').subscribe({
        next: (res) => alert(res.message),
        error: (err) => alert('Error deleting files: ' + err.message)
      });
    }
  }
}
