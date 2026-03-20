import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, ElementRef, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import Swal from 'sweetalert2';
import { CountService } from '../count-service';
import { SpinnerService } from '../../shared/spinner/spinner-service';
import { delay, tap } from 'rxjs/operators';
import { AnayseCountService,ApiResponse } from '../anayse-count-service';
import { ToastrService } from 'ngx-toastr';
import { saveAs } from 'file-saver';
import * as XLSX from 'xlsx-js-style'; 
@Component({
  selector: 'app-count-analysis-component',
  imports: [CommonModule,FormsModule],
  templateUrl: './count-analysis-component.html',
  styleUrl: './count-analysis-component.css',
})
export class CountAnalysisComponent {
@ViewChild('accDropdown') accDropdown!: ElementRef;
@ViewChild('hdwDropdown') hdwDropdown!: ElementRef;
@ViewChild('backorderDropdown') backorderDropdown!: ElementRef;

  selectedSource: string = 'snapshot';
  selectedHdwFile: string = "";
isAccLoadEnabled: boolean = true;
isImeiLoadEnabled: boolean = true;
  selectedAccFile: File | null = null;
excelHardware: boolean = false;
excelAccessory: boolean = false;
  accFilesList: string[] = [];
hdwFilesList: string[] = [];
  selectedFile: File | null = null;

    importedCounts: any[] = [];
      onhandNotCountedData: any[] = [];
  duplicateData: any[] = [];
  systemDuplicateData: any[] = [];
  cleanupPreviewData: any[] = [];
  invalidSerials: any[] = [];
  systemSerialData: any[] = [];
  discrepancyData: any[] = [];
  comparisonData: any[] = [];
  missingItemsData: any[] = [];
  notOnhandResults: any[] = [];
  assignWarehousePage: number = 1;
  checkDupilcatePage: number = 1;
  checkSystemDuplicatePage: number = 1;
  checkInvalidRecords: number = 1;
 pageSizes: number = 10;
  p: number = 1;
  selectedBackOrderFile: File | null = null;
   discrepanciesData: any[] = [];
   countType: 'hardware' | 'accessory' = 'hardware';
  warehouses: string[] = [];
 countFiles: any[] = []
   selectedLoadType: string = 'Both';
     loadSpireDatas: any;
    
  notInBVData: any[] = [];
  p2: number = 1;
  onhandNotCountedDatas: any[] = [];
  p3: number = 1;
  stockStatusDatas: any[] = [];
  p4: number = 1;
  backorderDatas: any[] = [];
  p5: number = 1;
  accEditData: any[] = [];
  accTotalItems: number = 0;

  selectedWarehouse: string = '';
  selectedCountFile: string = '';
  showModal = false;
fileSummary: any = null;

 startDate: string = '';
  endDate: string = '';
  statusMessage: string = '';
  isLoading: boolean = false;
  accessoryData: any[] = [];
  analysisData: any[] = [];
   totalItems = 0;
todayDate:any;



   constructor(
    private countService: CountService,
    private spinner: SpinnerService,
    private cdr: ChangeDetectorRef,
    private analyseService: AnayseCountService,
     private toaster: ToastrService,
  ) {}


  ngOnInit() {
  this.loadDropdowns();
  this.todayDate=new Date().toISOString().split('T')[0];
  }
onSourceChange() {
debugger
  if (this.selectedSource === 'snapshot') {

    // snapshot default
    this.isAccLoadEnabled = true;
    this.isImeiLoadEnabled = true;

    this.excelHardware = false;
    this.excelAccessory = false;

  } else {

    // excel default
    this.isAccLoadEnabled = false;
    this.isImeiLoadEnabled = false;

    this.excelHardware = true;
    this.excelAccessory = true;
  }

}
// onSourceChange() {
//   if (this.selectedSource === 'excel') {
//     this.loadDropdowns(); // sirf yahin call hoga
//   } else {
//     // reset dropdowns
//     this.hdwFilesList = [];
//     this.accFilesList = [];
//     this.selectedHdwFile = '';
//     this.selectedAccFile = null;
//   }
// }
onExcelHardwareChange() {

  if (this.excelHardware) {
    this.excelAccessory = false;
  }

}


loadDropdowns() {
debugger
  this.countService.getFileNames(false)
    .pipe(delay(0))
    .subscribe({
      next: (list) => {
        this.hdwFilesList = [...(list || [])];
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Error loading Hardware Files:', err);
        this.cdr.markForCheck();
      }
    });

  // 2. Load Accessory Files
  this.countService.getFileNames(true)
    .pipe(delay(0))
    .subscribe({
      next: (list) => {
        this.accFilesList = [...(list || [])];
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Error loading Accessory Files:', err);
        this.cdr.markForCheck();
      }
    });
}

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
    this.spinner.show();
    if (result.isConfirmed) {
      
      this.countService.deleteAllCounts(isACC).subscribe({
        next: (res) => {
          this.loadDropdowns();
          this.spinner.hide()
          Swal.fire('Deleted!', res.message, 'success');
if (isACC) this.selectedAccFile = null; else this.selectedHdwFile = "";
        },
        error: (err) => {
          this.spinner.hide()

          Swal.fire('Error', 'Failed to delete counts', 'error');
        }
      });
    }
  });
}

// ACCESSORY SELECT
onExcelAccessoryChange() {

  if (this.excelAccessory) {
    this.excelHardware = false;
  }

}



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
  this.spinner.show()
          const options = { 
            loadACC: this.isAccLoadEnabled, 
            loadIMEI: this.isImeiLoadEnabled 
          };
  
          this.countService.loadSnapshot(options).subscribe({
            next: (res: any) => {
              // this.loadDropdowns();
              this.spinner.hide()
              Swal.fire('Success', 'Snapshot Loaded successfully', 'success');
            },
            error: (err) => {
              Swal.fire('Error', 'Failed to load snapshot: ' + err.message, 'error');
            }
          });
        }
      });
    }
onAccFileSelected(event: any) {
  const file: File = event.target.files[0];
  if (file) {
    this.selectedAccFile = file;
  }
}

  //    onAccFileSelected(event: any) {
  //   this.selectedAccFile = event.target.files[0];
  // }
 importACCCounts() {
  debugger
  this.spinner.show();
    if (!this.selectedAccFile) 
      {
        this.spinner.hide()

        return;
      }
    this.analyseService.uploadACCCounts(this.selectedAccFile).subscribe({
      next: (res:any) => {
      this.toaster.success(res.message)
        this.loadDropdowns();
        this.selectedAccFile = null;

debugger
     if (this.accDropdown) {
    this.accDropdown.nativeElement.value = '';
  }
        this.spinner.hide()
      },
      error: (err) => {
        console.error(err);
        this.spinner.hide()
        alert("Upload failed!");
      }
    });
  }

   onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.selectedFile = input.files[0];
      console.log('Selected file:', this.selectedFile.name);
    }
  }


  
  uploadFile() {
    this.spinner.show()
    if (!this.selectedFile) {
      this.toaster.error("Please select a file first!");
      this.spinner.hide()
      return;
    }

    this.analyseService.importIMEICounts(this.selectedFile).subscribe({
      next: (res) => {
        this.toaster.success(res.message);
        this.loadDropdowns();
        this.selectedFile = null;
        if (this.hdwDropdown) {
        this.hdwDropdown.nativeElement.value = '';
      }
        this.spinner.hide()
      },
      error: (err) => {
        console.error(err);
        this.toaster.error("Error uploading file.");
        this.spinner.hide()

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

const fileNameValue = typeof fileName === 'string' ? fileName : fileName.name;

Swal.fire({
  title: 'Remove Counts?',
  text: `Are you sure you want to remove all counts from spreadsheet: ${fileNameValue}?`,
  icon: 'warning',
  showCancelButton: true,
  confirmButtonText: 'Yes, remove it',
  cancelButtonText: 'Cancel'
}).then((result) => {
  
  if (result.isConfirmed) {
    this.spinner.show()
    
    this.countService.deleteByFile(fileNameValue, isACC).subscribe({
      next: (res) => {
        Swal.fire('Deleted!', res.message, 'success');
        this.loadDropdowns();
        
        this.spinner.hide()
          if (isACC)
            this.selectedAccFile = null;
          else
            this.selectedHdwFile = "";

        },
        error: () => {
          Swal.fire('Error', 'Could not delete data', 'error');
        }
      });

    }

  });
}

onExportHardwareSheets() {
  debugger
    this.countService.exportHardwareSheets().subscribe({
      next: (blob: Blob) => {
        const dateStr = new Date().toISOString().split('T')[0];
        this.downloadFile(blob, `Hardware_Count_Sheet_${dateStr}.xlsx`);
        Swal.fire('Success', 'Hardware sheet downloaded!', 'success');
      },
      error: (err) => {
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

  onExportAccessorySheets() {
    debugger
    
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
  
        Swal.fire('Complete', 'Accessory Count Sheets Output complete', 'success');
      },
      error: (err) => {
        console.error(err);
        Swal.fire('Error', 'Export failed. Check if WWAccessories table has data.', 'error');
      }
    });
  }

   viewAllCounts(page: number = 1) {
    this.spinner.show()
    this.analyseService
      .getAllImportedCounts()
      .pipe(delay(0))
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.importedCounts = [...(res.result || [])];
            this.spinner.hide()
            this.exportToExcel(this.importedCounts,"Imported_Counts_Report")
          }
          this.cdr.markForCheck();
          this.spinner.hide()
        },
        error: () => {
          this.cdr.markForCheck();
          this.spinner.hide()

        }
      });
  }

   spireNotCounted(page: number = 1) {

    this.analyseService
      .getOnhandNotCounted()
      .pipe(delay(0))
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.onhandNotCountedData = [...(res.result || [])];
            this.exportToExcel(this.onhandNotCountedData,"IMEI BV Onhand")
          } else {
            console.error(res.message);
          }
          this.cdr.markForCheck();
        },
        error: (err) => {
          console.error(err);
          this.cdr.markForCheck();
        },
      });
  }



  checkDuplicates(page: number = 1) {
    this.checkDupilcatePage = page;

    this.analyseService.getDuplicateCounts(this.checkDupilcatePage, this.pageSizes).subscribe({
      next: (res) => {
        if (res.success) {
          this.duplicateData = res.result;
          if (this.duplicateData.length === 0 && this.p === 1) alert("No duplicate counts found!");
        }
        this.cdr.detectChanges();
      },
      error: () =>{}
    });
  }
  checkSystemDuplicates(page: number = 1) {
    this.checkSystemDuplicatePage
      = page;

    this.analyseService.getSystemDuplicates(this.checkSystemDuplicatePage, this.pageSizes).subscribe({
      next: (res) => {
        if (res.success) {
          this.systemDuplicateData = res.result;
          if (this.systemDuplicateData.length === 0 && this.p === 1) {
            this.toaster.warning("No duplicates found in System Onhand.");
          }
        }
        this.cdr.detectChanges();
      },
    });
  }
  findDuplicates() {
    this.spinner.show()
    this.analyseService.processDuplicates().subscribe({
    next: (res) => {
      if (res.success) {
        this.importedCounts = res.result; 

        Swal.fire({
          icon: 'success',
          title: 'Success!',
          text: res.message,
          timer: 2000,
          showConfirmButton: false
        });

        if (this.importedCounts && this.importedCounts.length > 0) {
          this.exportToExcel(this.importedCounts,"Duplicate_Report");
          this.spinner.hide()
        }
      } else {
        Swal.fire('Error', res.message, 'error');
        this.spinner.hide();
      }
    },
    error: (err:any) => {
      Swal.fire('Error', 'Server connection failed', 'error');
    }
  });
}
  showCleanupPreview() {
    this.spinner.show()
    this.analyseService.getCleanupPreview().subscribe({
      next: (res) => {
        if (res.success) {
          this.cleanupPreviewData = res.result;
          this.exportToExcel(this.cleanupPreviewData ,"Show_Duplicates")
        }
        this.spinner.hide()
      },
      error: () => this.spinner.hide()
    });
  }
  finalDeleteDuplicates() {
    if (confirm("Are you sure you want to permanently delete redundant duplicate counts? This cannot be undone.")) {
      this.spinner.show()
      this.analyseService.deleteDuplicates().subscribe({
        next: (res) => {
          if (res.success) {
            alert(res.message);
            this.cleanupPreviewData = [];
          }
          this.spinner.hide()
        },
        error: () => this.spinner.hide()
      });
    }
  }
  checkSerialIntegrity(page: number = 1) {
debugger
    this.analyseService.getInvalidSerials().subscribe({
      next: (res) => {
        if (res.success) {
          this.invalidSerials = res.result;
          if (this.invalidSerials.length === 0 && this.p === 1) alert("All serials are valid!");
        }
        if(this.invalidSerials.length>0)
        {
          this.exportToExcel(this.invalidSerials,"Invalid_Reports")
        }
        this.spinner.hide()
        this.cdr.detectChanges();
      },
      error: () => this.spinner.hide()
    });
  }

  checkSystemSerials() {
    this.spinner.show()
    this.analyseService.getSystemSerialVerify().subscribe({
      next: (res) => {
        if (res.success) {
          this.systemSerialData = res.result;
          this.exportToExcel(this.systemSerialData,"View_Invalid_Spire_Onhand")
        }
        this.spinner.hide()
      },
      error: () => this.spinner.hide()
    });
  }
  runDiscrepancyReport() {
    debugger
    this.analyseService.getDiscrepancyReport().subscribe({
      next: (res) => {
        debugger
       if (res.success && res.result && res.result.length > 0) {
        this.discrepancyData = res.result;
        console.log("isha",this.discrepancyData)
        this.exportToExcel(this.discrepancyData,"Discrepancy_Report");
      }
      },
      error: () => this.spinner.hide()
    });
  }

  runQtyComparison() {
    this.spinner.show()
    this.analyseService.getQtyVsSerialComparison().subscribe({
      next: (res) => {
        if (res.success) {
          this.comparisonData = res.result;
        }
        this.spinner.hide()
      },
      error: () => this.spinner.hide()
    });
  }
  fetchMissingItems() {
    this.spinner.show()
    this.analyseService.getMissingFromCount().subscribe({
      next: (res) => {
        if (res.success) {
          this.missingItemsData = res.result;
          this.exportToExcel(this.missingItemsData,"Spire_Onhand_Not_Counted")
        }
        this.spinner.hide()
      },
      error: () => this.spinner.hide()
    });
  }
  checkSpireStatus() {
    this.analyseService.processNotOnhandDetails().subscribe({
      next: (res) => {
        if (res.success) {
          this.notOnhandResults = res.result;
          this.exportToExcel( this.notOnhandResults,"Counted_but_not_onhand_in_Spire")
        }
      },
    });
  }

importBackOrders() {
  if (!this.selectedBackOrderFile) {
    Swal.fire('Error', 'Please select a file first', 'error');
    return;
  }

  this.spinner.show()
  this.analyseService.uploadBackOrders(this.selectedBackOrderFile).subscribe({
    next: (res) => {
      if (res.success) {
        Swal.fire('Success', 'Backorders imported successfully', 'success');
      }
       this.selectedBackOrderFile = null;

      if (this.backorderDropdown) {
        this.backorderDropdown.nativeElement.value = '';
      }
      this.spinner.hide()
    },
    error: (err) => {
      Swal.fire('Error', 'Import failed: ' + err.message, 'error');
      this.spinner.hide()
    }
  });
}


onBackOrderFileSelected(event: any) {
  const file = event.target.files[0];
  if (file) {
    this.selectedBackOrderFile = file;
  }
}

 fixLiveQty() { alert("Fix Live Quantities"); }

 loadCountFiles(countType:any) {
   debugger
   this.countType=countType
   this.spinner.show()
  this.analyseService.getCountFiles(this.countType).subscribe({
     next: (data) => {
       this.countFiles = data;
       console.log('Count Files loaded successfully:', this.countFiles);
      this.spinner.hide() 
       this.cdr.detectChanges();
     },
     error: (err) => {
       console.error('Error loading count files', err);
      this.spinner.hide() 
       
       alert('Error loading count files');
       this.cdr.detectChanges();
     }
   });
 }
  showSales() {
   this.spinner.show()
    this.accessoryData = [];

    this.analyseService.getItemSalesSummary()
      .pipe(
        delay(0),
        tap((res) => {
          debugger
          if (res.success && res.result) {
            this.accessoryData = [...res.result];
            this.spinner.hide()
            this.exportToExcel(this.accessoryData,"Sales_Reports")
            console.log("Sales Summary Data:", this.accessoryData.length);
          } else {
            this.accessoryData = [];
            if (!res.success)
              {
alert(res.message);
            this.spinner.hide()

              } 
                
          }
        })
      )
      .subscribe({
        next: () => {
          this.spinner.hide() 
          this.cdr.markForCheck();
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error("Sales Summary Error:", err);
         this.spinner.hide() 
          this.cdr.detectChanges();
        }
      });
  }
 
   loadSpireData() {
     debugger
     Swal.fire({
       title: 'Load Data?',
       text: `Sales/Receipts Loading Data: ${this.selectedLoadType}. `,
       icon: 'question',
       showCancelButton: true,
       confirmButtonText: 'Yes, Load it!'
     }).then((result) => {
       if (result.isConfirmed) {
         this.spinner.show()
         this.analyseService.loadSpireSalesReceipts(this.selectedLoadType).subscribe({
           next: (res) => {
             this.spinner.hide()
             if (res.success) {
               console.log("ishu",res)
               this.loadSpireDatas =res.result;
               Swal.fire('Loaded!', 'Sales and/or Receipts data loaded successfully.', 'success');
               this.exportToExcel(this.loadSpireDatas,"Reports")
             } else {
               Swal.fire('Error', res.message, 'error');
             }
           },
           error: () => {
             this.spinner.hide()
             Swal.fire('Error', 'Server error during data load.', 'error');
           }
         });
       }
     });
   }
 
 
   getDiscrepancies(page: any) {
     this.analyseService
       .getAccessoryDiscrepancies()
       .pipe(
         delay(300),
 
         tap((res) => {
           if (res.success && res.result) {
             this.discrepanciesData = [...res.result];
             this.cdr.detectChanges();
             this.exportToExcel(this.discrepanciesData,"Discrepancies")
           } else {
             this.discrepanciesData = [];
           }
         }),
 
 
       )
       .subscribe({
         error: (err) => {
           console.error("API Error:", err);
           this.discrepanciesData = [];
         }
       });
   }

 viewEditCounts() {
    debugger
    this.spinner.show()
    this.analyseService.getAccCountsEdit().subscribe({
      next: (res) => {
        this.accEditData = res.items.map(item => ({
          ...item,
          isEditing: false,
          tempQty: item.qtyTotal
        }));
        this.accTotalItems = res.totalItems;
        this.spinner.hide()
        this.cdr.detectChanges();
        this.exportToExcel(this.accEditData,"View_Edit_Counts")
      },
      error: () => this.spinner.hide()
    });
  }

    countedNotBV() {
    this.notInBVData = [];

    this.analyseService.getCountedNotInBV()
      .pipe(
        delay(0),
        tap((res) => {
          if (res.success && res.result) {
            console.log(res)
            this.notInBVData = [...res.result];
            this.p2 = 1;
            this.exportToExcel(this.notInBVData,"countedNotBV")
          } else {
            this.notInBVData = [];
            if (!res.success) console.error("API Error:", res.message);
          }
        })
      )
      .subscribe({
        next: () => {

          this.cdr.markForCheck();
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error("Connection Error:", err);
          this.cdr.detectChanges();
        }
      });
  }
  onhandNotCounteds() {
    this.onhandNotCountedDatas = [];

    this.analyseService.getOnhandNotCounteds()
      .pipe(
        delay(0),
        tap((res) => {
          if (res.success && res.result) {
            this.onhandNotCountedDatas = [...res.result];
            this.p3 = 1;
            console.log("Data loaded in onhandNotCountedDatas. Length:", this.onhandNotCountedDatas.length);
          } else {
            this.onhandNotCountedDatas = [];
          }
        })
      )
      .subscribe({
        next: () => {
          this.cdr.markForCheck();
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error("API Error:", err);
          this.cdr.detectChanges();
        }
      });
  }
  loadedStockStatus() {
    this.stockStatusDatas = [];

    this.analyseService.getLoadedStockStatus()
      .pipe(
        delay(0),
        tap((res) => {
          if (res.success && res.result) {
            this.stockStatusDatas = res.result.map((item: any) => ({
              ...item,
              invGroup: (typeof item.invGroup === 'object') ? 'N/A' : item.invGroup
            }));

            this.p4 = 1;
            this.exportToExcel( this.stockStatusDatas,"Loaded_ACC_Stock_Status")
            console.log("Data Prepared for UI:", this.stockStatusDatas.length);
            console.log("Data Prepared for UI:", this.stockStatusDatas);

          }
        })
      )
      .subscribe({
        next: () => {
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error("API Error:", err);
        }
      });
  }
 
 showReceipts() {
  this.spinner.show()
    if (!this.startDate || !this.endDate) {
      this.spinner.hide()
      alert("Please select dates first");
      return;
    }
    this.isLoading = true;
    this.accessoryData = [];
    this.analyseService.getItemReceiptsSummary(this.startDate, this.endDate)
      .pipe(
        delay(0),
        tap((res) => {
          if (res.success && res.result) {
            this.accessoryData = [...res.result];
            this.spinner.hide()
            this.exportToExcel(this.accessoryData,"Receipts_Reports")
            console.log("Receipts Data Loaded:", this.accessoryData.length);
          } else {
            this.accessoryData = [];
            this.spinner.hide()

            if (!res.success) alert(res.message);
          }
        })
      )
      .subscribe({
        next: () => {
          this.cdr.markForCheck();
            this.spinner.hide()

          this.cdr.detectChanges();
        },
        error: (err) => {
            this.spinner.hide()

          this.cdr.detectChanges();
        }
      });
  }
validateDates(): boolean {

  const today = new Date().toISOString().split('T')[0];

  if (!this.startDate || !this.endDate) {
    Swal.fire({
      icon: 'warning',
      title: 'Missing Dates',
      text: 'Please select both start date and end date.',
      confirmButtonColor: '#004d99'
    });
    return false;
  }

  if (this.endDate < this.startDate) {
    Swal.fire({
      icon: 'error',
      title: 'Invalid Date Range',
      text: 'End date cannot be earlier than Start date.',
      confirmButtonColor: '#004d99'
    });
    return false;
  }

  if (this.startDate > today || this.endDate > today) {
    Swal.fire({
      icon: 'warning',
      title: 'Invalid Date',
      text: 'Dates cannot be greater than today.',
      confirmButtonColor: '#004d99'
    });
    return false;
  }

  return true;
}

  getAccessorySalesByChannel() {
    this.spinner.show()
    if (!this.startDate || !this.endDate) {
      alert("Please select both Start and End dates");
      this.spinner.hide()
      return;
    }
    this.isLoading = true;
    this.accessoryData = [];

    this.analyseService.getAccessorySalesByChannel(this.startDate, this.endDate)
      .pipe(
        delay(0),
        tap((res: ApiResponse) => {
          if (res.success && res.result && res.result.length>0) {
            console.log("Channel Sales Data Received:", res.result.length);
            this.accessoryData = [...res.result];
            this.spinner.hide();
            this.exportToExcel(this.accessoryData,"Accessory_Sales_ByChannel")
          } else {
            this.accessoryData = [];
            this.spinner.hide();

            if (!res.success) alert("Error: " + res.message);
          }
        })
      )
      .subscribe({
        next: () => {
          this.isLoading = false;
          this.cdr.markForCheck();
          this.cdr.detectChanges();
            this.spinner.hide();

          console.log("UI Update Triggered for Channel Sales. Length:", this.accessoryData.length);
        },
        error: (err) => {
          console.error("API Error:", err);
          this.isLoading = false;
          this.accessoryData = [];
          this.cdr.detectChanges();
            this.spinner.hide();

          alert("Server error occurred");
        }
      });
  }

  loadAnalysis(page: number = 1) {
    // if (!this.startDate || !this.endDate) {
    //     Swal.fire({
    //   icon: 'warning',
    //   title: 'Missing Dates',
    //   text: 'Please select both start date and end date.',
    //   confirmButtonColor: '#004d99'
    // });

    // return;
  // }
 if (!this.validateDates()) return;
    

  

    this.analyseService.getAccessoryAnalysis(this.startDate, this.endDate)
      .pipe(
        delay(0),
        tap((res) => {
          if (res.success && res.result && res.result.length>0) {
            this.analysisData = [...res.result];
            this.totalItems = res.count || 0;
            this.exportToExcel( this.analysisData,"Analysis_Data")
          } else {
            this.analysisData = [];
          }
        })
      )
      .subscribe({
        next: () => {
          this.isLoading = false;
          this.cdr.markForCheck();
          this.cdr.detectChanges();
          console.log("UI Update Triggered. Length:", this.analysisData.length);
        },
        error: (err) => {
          console.error("API Error:", err);
          this.isLoading = false;
          this.cdr.detectChanges();
        }
      });
  }
confirmAssignment() {
    const payload = {
        CountFile: this.selectedCountFile,
        Warehouse: this.selectedWarehouse,
        CountType: this.countType
    };
    this.spinner.show();


    this.analyseService.assignCountsToWarehouse(payload).subscribe({
        next: (res) => {
            this.toaster.success('Updated Successfully!');
            this.spinner.hide()
            this.showModal = false;
            this.loadCountFiles(this.countType);
        },
        error: (err) => {
            this.spinner.hide()

            this.toaster.error('Error !.');
        }
    });
}

loadWarehouses() {
  this.analyseService.getWarehouses().subscribe({
    next: (data) => {
      this.warehouses = data;
      console.log('API Response Data:', this.warehouses); 
    },
    error: (err) => {
      console.error('Error loading warehouses', err);
      alert('Error loading warehouses');
    }
  });

  // Ye line API response se PEHLE chal jayegi, isliye ye [] dikhayegi
  console.log('Outside Subscribe (Immediate):', this.warehouses); 
}

  analyseAssignWarehouse()
{
  debugger
  this.showModal=true;
   this.loadWarehouses();
    this.loadCountFiles("Hardware");

    debugger
    this.showModal = true;
    this.fileSummary = null; 
this.selectedCountFile="Hardware"
   
setTimeout(() => {
  this.AnaylseEffect()
}, 2000);


  
}
AnaylseEffect()
{
  debugger
   
    if (!this.selectedCountFile) {
      alert('You must select a count file.');
      return;
    }

     // 3. API Call
    this.analyseService.getCountFileSummary(this.selectedCountFile, this.countType)
        .subscribe({
            next: (res) => {
                console.log("Response from API:", res); // Isko console mein check karo
                this.fileSummary = res; 
            },
            error: (err) => {
                this.showModal = false;
                alert("Summary load nahi ho saki.");
            }
        });

}

  // 2 ROW

  onCountTypeChange() {
debugger
  this.loadCountFiles(this.countType);

}

selectRow(item: any) {
  debugger
    this.fileSummary = item;
    this.selectedCountFile = item.countFile; 
    console.log("File Selected:", item.countFile);
}


  // exportToExcel(data: any[], name?: string) {
  //   if (!data || data.length === 0) {
  //     Swal.fire({ icon: 'info', title: 'No Data', text: 'No Data Found To Export' });
  //     return;
  //   }
  //   const filename = (name || 'Full_Counts_Report').replace(/\s+/g, '_') + '.xlsx';
  //   const ws = XLSX.utils.json_to_sheet(data);
  //   ws['!cols'] = Object.keys(data[0]).map(() => ({ wch: 20 }));
  //   const wb = XLSX.utils.book_new();
  //   XLSX.utils.book_append_sheet(wb, ws, 'Data');
  //   const buffer = XLSX.write(wb, { bookType: 'xlsx', type: 'array' });
  //   saveAs(new Blob([buffer], { type: 'application/octet-stream' }), filename);
  //   Swal.fire({ icon: 'success', title: 'Excel Exported', timer: 1500, showConfirmButton: false });
  // }
exportToExcel(data: any[], name?: string) {
  debugger
  if (!data || data.length === 0) {
    Swal.fire({ icon: 'info', title: 'No Data', text: 'No Data Found To Export' });
    return;
  }

  const filename = (name || 'Full_Counts_Report').replace(/\s+/g, '_') + '.xlsx';

  const headerKeys = Object.keys(data[0]);
  const formattedHeaders = headerKeys.map(key =>
    key
      .replace(/([A-Z])/g, ' $1') 
      .replace(/^./, str => str.toUpperCase())
      .trim()
  );

  // Create worksheet from data
  const dataRows = data.map(row => headerKeys.map(k => row[k]));
  const aoa = [formattedHeaders, ...dataRows];
  const ws = XLSX.utils.aoa_to_sheet(aoa);

  // Apply bold style to header row (A1, B1, C1, etc.)
  const range = XLSX.utils.decode_range(ws['!ref']!);
  for (let col = range.s.c; col <= range.e.c; col++) {
    const cellAddress = XLSX.utils.encode_cell({ r: 0, c: col }); // Row 0 = header
    if (ws[cellAddress]) {
      ws[cellAddress].s = {
        font: { bold: true }
      };
    }
  }

  // Set column widths
  ws['!cols'] = headerKeys.map(() => ({ wch: 20 }));

  const wb = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(wb, ws, 'Data');

  const buffer = XLSX.write(wb, { bookType: 'xlsx', type: 'array' });
  saveAs(new Blob([buffer], { type: 'application/octet-stream' }), filename);

  Swal.fire({ icon: 'success', title: 'Excel Exported', timer: 1500, showConfirmButton: false });
}

}
