import { CommonModule, NgIf } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, DebugElement } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AnayseCountService, ApiResponse } from '../inventory/anayse-count-service';
import { ChangeDetectorRef } from '@angular/core';
import { of } from 'rxjs';
import { delay, tap } from 'rxjs/operators';
import Swal from 'sweetalert2';
import { Console } from 'console';
import * as XLSX from 'xlsx';
import { saveAs } from 'file-saver';
import { SpinnerService } from '../shared/spinner/spinner-service';
@Component({
  selector: 'app-analyse-count-component',
  imports: [CommonModule, FormsModule,NgIf],
  templateUrl: './analyse-count-component.html',
  styleUrl: './analyse-count-component.css',
})
export class AnalyseCountComponent {
  loadSpireDatas: any;


  constructor(private countService: AnayseCountService, private cdr: ChangeDetectorRef,private spinner:SpinnerService) {

  }


  // 3 ROW
  startDate: string = '';
  endDate: string = '';
  statusMessage: string = '';
  isLoading: boolean = false;
  accessoryData: any[] = [];
  analysisData: any[] = [];

  // 1 ROW
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
  selectedFile: File | null = null;
  totalItems = 0;
  currentPage = 1;
  totalPages = 0;
  pageSize = 25;
  pages: number[] = [];
  currentImportPage: number = 1;
  activeTab: string = '';
  imeiOnHand: number = 1;
  assignWarehouseData: any[] = [];
  assignWarehousePage: number = 1;
  checkDupilcatePage: number = 1;
  checkSystemDuplicatePage: number = 1;
  checkInvalidRecords: number = 1;


  // 2 ROW
  selectedLoadType: string = 'Both';
  discrepanciesData: any[] = [];
  p: number = 1;
  pageSizes: number = 10;
  notInBVData: any[] = [];
  p2: number = 1;
  onhandNotCountedDatas: any[] = [];
  p3: number = 1;
  stockStatusDatas: any[] = [];
  p4: number = 1;
  backorderDatas: any[] = [];
  p5: number = 1;
  selectedAccFile: File | null = null;
  accEditData: any[] = [];
  accTotalItems: number = 0;
  selectedBackOrderFile: File | null = null;
   countType: 'hardware' | 'accessory' = 'hardware';
  warehouses: string[] = [];
 countFiles: any[] = []

  selectedWarehouse: string = '';
  selectedCountFile: string = '';
  showModal = false;
fileSummary: any = null;

  // 2 ROW



  countFolder: string = 'V:\\inventorycounts-Spire\\counts';



AnaylseEffect()
{
  debugger
   
    if (!this.selectedCountFile) {
      alert('You must select a count file.');
      return;
    }

     // 3. API Call
    this.countService.getCountFileSummary(this.selectedCountFile, this.countType)
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
confirmAssignment() {
    const payload = {
        CountFile: this.selectedCountFile,
        Warehouse: this.selectedWarehouse,
        CountType: this.countType
    };
    this.spinner.show();

    console.log("Sending Payload:", payload); 

    this.countService.assignCountsToWarehouse(payload).subscribe({
        next: (res) => {
            alert('Updated Successfully!');
            this.spinner.hide()
            this.showModal = false;
            this.loadCountFiles(this.countType);
        },
        error: (err) => {
            console.error("400 Error Details:", err);
            this.spinner.hide()

            alert('Data format galat hai (400 Bad Request).');
        }
    });
}

loadWarehouses() {
  this.countService.getWarehouses().subscribe({
    next: (data) => {
      this.warehouses = data; // Data yahan assign hua
      console.log('API Response Data:', this.warehouses); // Yahan data dikhega
    },
    error: (err) => {
      console.error('Error loading warehouses', err);
      alert('Error loading warehouses');
    }
  });

  // Ye line API response se PEHLE chal jayegi, isliye ye [] dikhayegi
  console.log('Outside Subscribe (Immediate):', this.warehouses); 
}

 loadCountFiles(countType:any) {
  debugger
  this.countType=countType
  this.spinner.show()
 this.countService.getCountFiles(this.countType).subscribe({
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
        this.isLoading = true;
        this.countService.loadSpireSalesReceipts(this.selectedLoadType).subscribe({
          next: (res) => {
            this.isLoading = false;
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
            this.isLoading = false;
            Swal.fire('Error', 'Server error during data load.', 'error');
          }
        });
      }
    });
  }


  getDiscrepancies(page: any) {
    this.countService
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

  nextPage() {
    this.p++;
    this.getDiscrepancies(this.p);
  }

  prevPage() {
    if (this.p > 1) {
      this.p--;
      this.getDiscrepancies(this.p);
    }
  }
  countedNotBV() {
    this.notInBVData = [];

    this.countService.getCountedNotInBV()
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

    this.countService.getOnhandNotCounteds()
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

    this.countService.getLoadedStockStatus()
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



  uploadBackorders(file: File) {
    this.isLoading = true;

    this.countService.importBackorders(file)
      .pipe(
        delay(0),
        tap(res => {
          if (res.success) {
            this.backorderDatas = [...res.result];
            this.p5 = 1;
            alert(res.message || "Data Imported Successfully!");
          }
        })
      )
      .subscribe({
        next: () => {
          this.isLoading = false;
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.isLoading = false;
          console.error("Upload Error:", err);
          this.cdr.detectChanges();
        }
      });
  }


onBackOrderFileSelected(event: any) {
  const file = event.target.files[0];
  if (file) {
    this.selectedBackOrderFile = file;
  }
}

importBackOrders() {
  if (!this.selectedBackOrderFile) {
    Swal.fire('Error', 'Please select a file first', 'error');
    return;
  }

  this.isLoading = true;
  this.countService.uploadBackOrders(this.selectedBackOrderFile).subscribe({
    next: (res) => {
      if (res.success) {
        Swal.fire('Success', 'Backorders imported successfully', 'success');
      }
      this.isLoading = false;
    },
    error: (err) => {
      Swal.fire('Error', 'Import failed: ' + err.message, 'error');
      this.isLoading = false;
    }
  });
}









  // 2 Row

  importCounts() { alert("Import Counts Clicked"); }


  viewIMEIDuplicates() { alert("View IMEI Duplicates Counted"); }
  viewIMEIDuplicatesBV() { alert("View IMEI Duplicates BV Onhand"); }
  showDuplicates() { alert("Show Duplicates"); }

  viewInvalidCounts() { alert("View Invalid Counts"); }

  viewPartDiscrepancies() { alert("View Part No Discrepancies"); }
  searchReplaceSKU() { alert("Search and Replace SKU"); }

  compareBVtoActual() { alert("Compare BV SN Counts to Actual Counts"); }
  compareActualtoBV() { alert("Compare Actual Counts to BV SN Counts"); }


  countedNotSpire() { alert("Counted but not onhand in Spire"); }
  availableVsSpire() { alert("Available SN Versus Spire Onhand Qty"); }
  fixLiveQty() { alert("Fix Live Quantities"); }

  onAccFileSelected(event: any) {
    this.selectedAccFile = event.target.files[0];
  }

  importACCCounts() {
    if (!this.selectedAccFile) return;

    this.isLoading = true;
    this.countService.uploadACCCounts(this.selectedAccFile).subscribe({
      next: (res) => {
        alert(res.message);
        this.isLoading = false;
        this.selectedAccFile = null;
      },
      error: (err) => {
        console.error(err);
        alert("Upload failed!");
        this.isLoading = false;
      }
    });
  }
  viewEditCounts() {
    debugger
    this.countService.getAccCountsEdit().subscribe({
      next: (res) => {
        this.accEditData = res.items.map(item => ({
          ...item,
          isEditing: false,
          tempQty: item.qtyTotal
        }));
        this.accTotalItems = res.totalItems;
        this.cdr.detectChanges();
        this.exportToExcel(this.accEditData,"View_Edit_Counts")
      },
      error: () => this.isLoading = false
    });
  }

  saveAccEdit(item: any) {
    this.countService.updateAccQty(item.id, item.tempQty).subscribe({
      next: (res) => {
        item.qtyTotal = item.tempQty; // UI update
        item.isEditing = false;
        Swal.fire('Saved!', 'Quantity updated successfully.', 'success');
      },
      error: () => Swal.fire('Error', 'Failed to update.', 'error')
    });
  }

  cancelAccEdit(item: any) {
    item.tempQty = item.qtyTotal;
    item.isEditing = false;
  }




  // 2 Row

  accCountFolder = 'V:\\inventorycounts-Spire\\ACCcounts';
  accBOFolder = 'V:\\inventorycounts-Spire\\ACCBackOrderCounts';


  // 1 ROW



  // 1 ROW
  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.selectedFile = input.files[0];
      console.log('Selected file:', this.selectedFile.name);
    }
  }

  uploadFile() {
    if (!this.selectedFile) {
      alert("Please select a file first!");
      return;
    }

    this.countService.importIMEICounts(this.selectedFile).subscribe({
      next: (res) => {
        alert(res.message);
        this.selectedFile = null;
      },
      error: (err) => {
        console.error(err);
        alert("Error uploading file.");
      }
    });
  }

  viewAllCounts(page: number = 1) {
    this.activeTab = 'counts';
    this.countService
      .getAllImportedCounts()
      .pipe(delay(0))
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.importedCounts = [...(res.result || [])];
            this.exportToExcel(this.importedCounts,"Imported_Counts_Report")
          }
          this.cdr.markForCheck();
        },
        error: () => {
          this.isLoading = false;
          this.cdr.markForCheck();
        }
      });
  }


  spireNotCounted(page: number = 1) {
    this.activeTab = 'onhand';

    this.countService
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
          this.isLoading = false;
          this.cdr.markForCheck();
        },
        error: (err) => {
          console.error(err);
          this.isLoading = false;
          this.cdr.markForCheck();
        },
      });
  }
  assignWarehouses(page: number = 1) {
    this.activeTab = 'assign';
    this.assignWarehousePage = page;
    this.isLoading = true;

    this.countService
      .getWarehouseAssignments(this.assignWarehousePage, this.pageSizes)
      .pipe(delay(0))
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.assignWarehouseData = [...(res.result || [])];
          } else {
            console.error(res.message);
          }
          this.isLoading = false;
          this.cdr.markForCheck();
        },
        error: (err) => {
          console.error(err);
          this.isLoading = false;
          this.cdr.markForCheck();
        }
      });
  }
  changePage(step: number) {
    this.p += step;

    if (this.activeTab === 'counts') {
      this.viewAllCounts(this.currentImportPage);
    } else if (this.activeTab === 'onhand') {
      this.spireNotCounted(this.p);
    } else if (this.activeTab === 'assign') {
      this.assignWarehouses(this.assignWarehousePage);
    }
    else if (this.activeTab === 'duplicates') this.checkDuplicates(this.checkDupilcatePage);
    else if (this.activeTab === 'systemDuplicates') this.checkSystemDuplicates(this.checkSystemDuplicatePage);
    else if (this.activeTab === 'invalid') this.checkSerialIntegrity(this.checkInvalidRecords);
  }

  checkDuplicates(page: number = 1) {
    this.activeTab = 'duplicates';
    this.checkDupilcatePage = page;
    this.isLoading = true;

    this.countService.getDuplicateCounts(this.checkDupilcatePage, this.pageSizes).subscribe({
      next: (res) => {
        if (res.success) {
          this.duplicateData = res.result;
          if (this.duplicateData.length === 0 && this.p === 1) alert("No duplicate counts found!");
        }
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => this.isLoading = false
    });
  }
  checkSystemDuplicates(page: number = 1) {
    this.activeTab = 'systemDuplicates';
    this.checkSystemDuplicatePage
      = page;
    this.isLoading = true;

    this.countService.getSystemDuplicates(this.checkSystemDuplicatePage, this.pageSizes).subscribe({
      next: (res) => {
        if (res.success) {
          this.systemDuplicateData = res.result;
          if (this.systemDuplicateData.length === 0 && this.p === 1) {
            alert("No duplicates found in System Onhand.");
          }
        }
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => this.isLoading = false
    });
  }
  findDuplicates() {
    this.isLoading = true;
    this.countService.processDuplicates().subscribe({
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
        }
      } else {
        Swal.fire('Error', res.message, 'error');
      }
    },
    error: (err) => {
      Swal.fire('Error', 'Server connection failed', 'error');
    }
  });
}
  showCleanupPreview() {
    this.isLoading = true;
    this.countService.getCleanupPreview().subscribe({
      next: (res) => {
        if (res.success) {
          this.cleanupPreviewData = res.result;
          this.exportToExcel(this.cleanupPreviewData ,"Show_Duplicates")
        }
        this.isLoading = false;
      },
      error: () => this.isLoading = false
    });
  }
  finalDeleteDuplicates() {
    if (confirm("Are you sure you want to permanently delete redundant duplicate counts? This cannot be undone.")) {
      this.isLoading = true;
      this.countService.deleteDuplicates().subscribe({
        next: (res) => {
          if (res.success) {
            alert(res.message);
            this.cleanupPreviewData = [];
          }
          this.isLoading = false;
        },
        error: () => this.isLoading = false
      });
    }
  }
  checkSerialIntegrity(page: number = 1) {
    this.activeTab = 'invalid';
debugger
    this.countService.getInvalidSerials().subscribe({
      next: (res) => {
        if (res.success) {
          this.invalidSerials = res.result;
          if (this.invalidSerials.length === 0 && this.p === 1) alert("All serials are valid!");
        }
        if(this.invalidSerials.length>0)
        {
          this.exportToExcel(this.invalidSerials,"Invalid_Reports")
        }
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => this.isLoading = false
    });
  }

  checkSystemSerials() {
    this.isLoading = true;
    this.countService.getSystemSerialVerify().subscribe({
      next: (res) => {
        if (res.success) {
          this.systemSerialData = res.result;
          this.exportToExcel(this.systemSerialData,"View_Invalid_Spire_Onhand")
        }
        this.isLoading = false;
      },
      error: () => this.isLoading = false
    });
  }
  runDiscrepancyReport() {
    debugger
    this.countService.getDiscrepancyReport().subscribe({
      next: (res) => {
        debugger
       if (res.success && res.result && res.result.length > 0) {
        this.discrepancyData = res.result;
        console.log("isha",this.discrepancyData)
        this.exportToExcel(this.discrepancyData,"Discrepancy_Report");
      }
      },
      error: () => this.isLoading = false
    });
  }

  runQtyComparison() {
    this.isLoading = true;
    this.countService.getQtyVsSerialComparison().subscribe({
      next: (res) => {
        if (res.success) {
          this.comparisonData = res.result;
        }
        this.isLoading = false;
      },
      error: () => this.isLoading = false
    });
  }
  fetchMissingItems() {
    this.isLoading = true;
    this.countService.getMissingFromCount().subscribe({
      next: (res) => {
        if (res.success) {
          this.missingItemsData = res.result;
          this.exportToExcel(this.missingItemsData,"Spire_Onhand_Not_Counted")
        }
        this.isLoading = false;
      },
      error: () => this.isLoading = false
    });
  }
  checkSpireStatus() {
    this.isLoading = true;
    this.countService.processNotOnhandDetails().subscribe({
      next: (res) => {
        if (res.success) {
          this.notOnhandResults = res.result;
          this.exportToExcel( this.notOnhandResults,"Counted_but_not_onhand_in_Spire")
        }
        this.isLoading = false;
      },
      error: () => this.isLoading = false
    });
  }




  onhandNotCounted() { alert("Onhand BV Not Counted"); }
  undoFixes() { alert("Undo ALL Fixes"); }


  // 3 ROW
  showReceipts() {
    if (!this.startDate || !this.endDate) {
      alert("Please select dates first");
      return;
    }
    this.isLoading = true;
    this.accessoryData = [];
    this.countService.getItemReceiptsSummary(this.startDate, this.endDate)
      .pipe(
        delay(0),
        tap((res) => {
          if (res.success && res.result) {
            this.accessoryData = [...res.result];
            this.exportToExcel(this.accessoryData,"Receipts_Reports")
            console.log("Receipts Data Loaded:", this.accessoryData.length);
          } else {
            this.accessoryData = [];
            if (!res.success) alert(res.message);
          }
        })
      )
      .subscribe({
        next: () => {
          this.isLoading = false;
          this.cdr.markForCheck();
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error("Receipts API Error:", err);
          this.isLoading = false;
          this.cdr.detectChanges();
        }
      });
  }
  getAccessorySalesByChannel() {
    if (!this.startDate || !this.endDate) {
      alert("Please select both Start and End dates");
      return;
    }
    this.isLoading = true;
    this.accessoryData = [];

    this.countService.getAccessorySalesByChannel(this.startDate, this.endDate)
      .pipe(
        delay(0),
        tap((res: ApiResponse) => {
          if (res.success && res.result && res.result.length>0) {
            console.log("Channel Sales Data Received:", res.result.length);
            this.accessoryData = [...res.result];
            this.exportToExcel(this.accessoryData,"Accessory_Sales_ByChannel")
          } else {
            this.accessoryData = [];
            if (!res.success) alert("Error: " + res.message);
          }
        })
      )
      .subscribe({
        next: () => {
          this.isLoading = false;
          this.cdr.markForCheck();
          this.cdr.detectChanges();
          console.log("UI Update Triggered for Channel Sales. Length:", this.accessoryData.length);
        },
        error: (err) => {
          console.error("API Error:", err);
          this.isLoading = false;
          this.accessoryData = [];
          this.cdr.detectChanges();
          alert("Server error occurred");
        }
      });
  }


  showSales() {
   
    this.accessoryData = [];

    this.countService.getItemSalesSummary()
      .pipe(
        delay(0),
        tap((res) => {
          debugger
          if (res.success && res.result) {
            this.accessoryData = [...res.result];
            this.exportToExcel(this.accessoryData,"Sales_Reports")
            console.log("Sales Summary Data:", this.accessoryData.length);
          } else {
            this.accessoryData = [];
            if (!res.success) alert(res.message);
          }
        })
      )
      .subscribe({
        next: () => {
          this.isLoading = false;
          this.cdr.markForCheck();
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error("Sales Summary Error:", err);
          this.isLoading = false;
          this.cdr.detectChanges();
        }
      });
  }
  loadAnalysis(page: number = 1) {
    if (!this.startDate || !this.endDate) {
      alert("Please select both dates");
      return;
    }

  

    this.countService.getAccessoryAnalysis(this.startDate, this.endDate)
      .pipe(
        delay(0),
        tap((res) => {
          if (res.success && res.result && res.result.length>0) {
            this.analysisData = [...res.result];
            this.totalItems = res.count || 0;
            this.exportToExcel( this.analysisData,"Analysis_Data")
            this.totalPages = res.activity || 0;
            this.updatePages();
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
  private updatePages() {
    this.pages = [];
    const maxVisiblePages = 5;
    let startPage = Math.max(1, this.currentPage - Math.floor(maxVisiblePages / 2));
    let endPage = Math.min(this.totalPages, startPage + maxVisiblePages - 1);

    if (endPage - startPage + 1 < maxVisiblePages) {
      startPage = Math.max(1, endPage - maxVisiblePages + 1);
    }

    for (let i = startPage; i <= endPage; i++) {
      this.pages.push(i);
    }
  }

// exportToExcel(name?: string) {
//   if (!this.importedCounts || this.importedCounts.length === 0) {
//     Swal.fire({ icon: 'info', title: 'No Data', text: 'No Data Found To Export' });
//     return;
//   }

//   const filename = (name || 'Full_Counts_Report').replace(/\s+/g, '_') + '.xlsx';
  
//   // Frontend Excel Logic
//   const ws = XLSX.utils.json_to_sheet(this.importedCounts);
  
//   // Column width auto-adjust (Approx 20 chars)
//   ws['!cols'] = Object.keys(this.importedCounts[0]).map(() => ({ wch: 20 }));

//   const wb = XLSX.utils.book_new();
//   XLSX.utils.book_append_sheet(wb, ws, 'Data');

//   const buffer = XLSX.write(wb, { bookType: 'xlsx', type: 'array' });
//   saveAs(new Blob([buffer], { type: 'application/octet-stream' }), filename);
  
//   Swal.fire({ icon: 'success', title: 'Excel Exported', timer: 1500, showConfirmButton: false });
// }
exportToExcel(data: any[], name?: string) {
  // Check the 'data' parameter instead of 'this.importedCounts'
  if (!data || data.length === 0) {
    Swal.fire({ icon: 'info', title: 'No Data', text: 'No Data Found To Export' });
    return;
  }

  const filename = (name || 'Full_Counts_Report').replace(/\s+/g, '_') + '.xlsx';
  
  const ws = XLSX.utils.json_to_sheet(data);
  
  ws['!cols'] = Object.keys(data[0]).map(() => ({ wch: 20 }));

  const wb = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(wb, ws, 'Data');

  const buffer = XLSX.write(wb, { bookType: 'xlsx', type: 'array' });
  saveAs(new Blob([buffer], { type: 'application/octet-stream' }), filename);
  
  Swal.fire({ icon: 'success', title: 'Excel Exported', timer: 1500, showConfirmButton: false });
}
}