import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CountService } from '../count-service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import Swal from 'sweetalert2';
import { SpinnerService } from '../../shared/spinner/spinner-service';
import { of } from 'rxjs';
import { delay, tap } from 'rxjs/operators';


@Component({
  selector: 'app-inventory-count-component',
  standalone: true, // Ensure standalone if using latest Angular
  imports: [CommonModule, FormsModule],
  templateUrl: './inventory-count-component.html',
  styleUrl: './inventory-count-component.css',
})
export class InventoryCountComponent implements OnInit {
  selectedAccFile: string = "";
  selectedHdwFile: string = "";
  processStatus: string = "Ready";
  isAccLoadEnabled: boolean = true;
  isImeiLoadEnabled: boolean = true;

  // Inventory Dates
  currentSerialDate: string = "";
  currentInventDate: string = "";
  lastNightSerialDate: string = "";
  lastNightInventDate: string = "";

  accFilesList: string[] = [];
hdwFilesList: string[] = [];

  constructor(
    private countService: CountService,
    private spinner: SpinnerService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    // this.refreshFileDates();
  this.loadDropdowns();
  }


//   loadDropdowns() {
//   // Load Hardware Files
//   this.countService.getFileNames(false).subscribe(list => {
//     this.hdwFilesList = list;
//   });

//   // Load Accessory Files
//   this.countService.getFileNames(true).subscribe(list => {
//     this.accFilesList = list;
//   });
// }

loadDropdowns() {

  this.countService.getFileNames(false)
    .pipe(delay(0))
    .subscribe({
      next: (list) => {
        this.hdwFilesList = [...(list || [])];
        this.checkLoadingStatus();
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Error loading Hardware Files:', err);
        this.checkLoadingStatus();
        this.cdr.markForCheck();
      }
    });

  // 2. Load Accessory Files
  this.countService.getFileNames(true)
    .pipe(delay(0))
    .subscribe({
      next: (list) => {
        this.accFilesList = [...(list || [])];
        this.checkLoadingStatus();
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Error loading Accessory Files:', err);
        this.checkLoadingStatus();
        this.cdr.markForCheck();
      }
    });
}

private checkLoadingStatus() {
  if (this.hdwFilesList && this.accFilesList) {
  }
}
  // --- REUSABLE LOADER HELPERS ---
  private showLoading(msg: string) {
    this.processStatus = msg;
    this.spinner.show();
    this.cdr.detectChanges();
  }

  private hideLoading() {
    this.spinner.hide();
    this.cdr.detectChanges();
  }

  // --- DATA OPERATIONS (SNAPSHOT) ---
  onLoadLiveSnapshot() {
    debugger
    Swal.fire({
      title: 'Are you sure?',
      text: "Do you wish to load a Snapshot of current LIVE Spire data?",
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#004d99',
      cancelButtonColor: '#d33',
      confirmButtonText: 'Yes, Load it!'
    }).then((result) => {
      if (result.isConfirmed) {
        debugger
        this.showLoading("Loading Live Snapshot...");

        const options = { 
          loadACC: this.isAccLoadEnabled, 
          loadIMEI: this.isImeiLoadEnabled 
        };

        this.countService.loadSnapshot(options).subscribe({
          next: (res: any) => {
            this.hideLoading();
            this.processStatus = "Snapshot Loaded Successfully";
            Swal.fire('Success', 'Snapshot Loaded successfully', 'success');
            this.loadDropdowns();
          },
          error: (err) => {
            this.hideLoading();
            this.processStatus = "Failed to load snapshot";
            Swal.fire('Error', 'Failed to load snapshot: ' + err.message, 'error');
          }
        });
      }
    });
  }


  onDeleteByFile(isACC: boolean) {
    debugger
  const fileName = isACC ? this.selectedAccFile : this.selectedHdwFile;
  const type = isACC ? 'ACC' : 'Hardware';

  if (!fileName) {
    Swal.fire('Error', 'Please select a file first', 'error');
    return;
  }

  Swal.fire({
    title: 'Remove Counts?',
    text: `Are you sure you want to remove all counts from spreadsheet: ${fileName}?`,
    icon: 'warning',
    showCancelButton: true,
    confirmButtonText: 'Yes, remove it',
    cancelButtonText: 'Cancel'
  }).then((result) => {
    if (result.isConfirmed) {
      this.showLoading(`Deleting ${type} counts...`);
      
      this.countService.deleteByFile(fileName, isACC).subscribe({
        next: (res) => {
          this.hideLoading();
          Swal.fire('Deleted!', res.message, 'success');
          this.loadDropdowns();
          if (isACC) this.selectedAccFile = ""; else this.selectedHdwFile = "";
          
        },
        error: (err) => {
          this.hideLoading();
          Swal.fire('Error', 'Could not delete data', 'error');
        }
      });
    }
  });
}

  // --- ACTIONS WITH SWAL ---
onDeleteAllCounts(isACC: boolean) {
  debugger
  const type = isACC ? 'Accessories' : 'Hardware';

  Swal.fire({
    title: 'Are you sure?',
    text: `You want to remove ALL ${type} counts? This cannot be undone!`,
    icon: 'warning',
    showCancelButton: true,
    confirmButtonColor: '#d33',
    cancelButtonColor: '#3085d6',
    confirmButtonText: 'Yes, delete all!'
  }).then((result) => {
    if (result.isConfirmed) {
      this.showLoading(`Deleting all ${type} counts...`);
      
      this.countService.deleteAllCounts(isACC).subscribe({
        next: (res) => {
          this.hideLoading();
          Swal.fire('Deleted!', res.message, 'success');
this.loadDropdowns();
if (isACC) this.selectedAccFile = ""; else this.selectedHdwFile = "";
        },
        error: (err) => {
          this.hideLoading();
          Swal.fire('Error', 'Failed to delete counts', 'error');
        }
      });
    }
  });
}

  // --- DATES REFRESH ---
refreshFileDates() {
  debugger
  this.showLoading("Refreshing Dates...");
  this.countService.getFileStatus().subscribe({
    next: (res: any) => {
      console.log("Response Check:", res);
      
      if (res && res.result && res.result.result) {
        const data = res.result.result; 

        this.currentSerialDate = data.serialCurrent;
        this.currentInventDate = data.inventoryCurrent;
        this.lastNightSerialDate = data.serialLastNight;
        this.lastNightInventDate = data.inventoryLastNight;

        this.processStatus = "Dates Checked";
      } else {
        this.processStatus = "Invalid Response Format";
      }
      this.hideLoading();
    },
    error: (err) => {
      this.hideLoading();
      this.processStatus = "Error connecting to server";
    }
  });
}

  // --- EXPORT LOGIC ---
onExportHardwareSheets() {
  debugger
    this.showLoading("Generating Hardware Excel...");
    this.countService.exportHardwareSheets().subscribe({
      next: (blob: Blob) => {
        const dateStr = new Date().toISOString().split('T')[0];
        this.downloadFile(blob, `Hardware_Count_Sheet_${dateStr}.xlsx`);
        this.hideLoading();
        this.processStatus = "Hardware Sheet Downloaded";
        Swal.fire('Success', 'Hardware sheet downloaded!', 'success');
      },
      error: (err) => {
        this.hideLoading();
        Swal.fire('Error', 'Export failed. Database might be empty.', 'error');
      }
    });
  }

 

  private downloadFile(blob: Blob, fileName: string) {
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.click();
    window.URL.revokeObjectURL(url);
  }

  onSyncLastNightFiles() {
  Swal.fire({
    title: 'Sync Files?',
    text: "Confirm file dates before syncing. Continue?",
    icon: 'info',
    showCancelButton: true,
    confirmButtonText: 'Yes, Sync'
  }).then((result) => {
    if (result.isConfirmed) {
      this.showLoading("Syncing inventory files...");
      this.countService.syncInventoryFiles().subscribe({
        next: () => {
          this.refreshFileDates(); // Sync ke baad dates refresh karein
          this.hideLoading();
          Swal.fire('Synced', 'Data Copied and Dates Refreshed', 'success');
        },
        error: (err) => {
          this.hideLoading();
          Swal.fire('Error', 'Sync failed: ' + err.message, 'error');
        }
      });
    }
  });
}

onExportAccessorySheets() {
  debugger
  this.showLoading("Generating Accessory Excel Sheets...");
  
  this.countService.exportAccessorySheets().subscribe({
    next: (blob: Blob) => {
      const dateStr = new Date().toISOString().split('T')[0];
      const fileName = `Accessory_Count_Sheets_${dateStr}.xlsx`;
      
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = fileName;
      link.click();
      window.URL.revokeObjectURL(url);

      this.hideLoading();
      this.processStatus = "Accessory Sheets Downloaded";
      Swal.fire('Complete', 'Accessory Count Sheets Output complete', 'success');
    },
    error: (err) => {
      this.hideLoading();
      console.error(err);
      Swal.fire('Error', 'Export failed. Check if WWAccessories table has data.', 'error');
    }
  });
}
}