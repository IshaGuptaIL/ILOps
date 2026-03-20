import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InventoryDashboardComponent } from './inventory-dashboard-component';

describe('InventoryDashboardComponent', () => {
  let component: InventoryDashboardComponent;
  let fixture: ComponentFixture<InventoryDashboardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InventoryDashboardComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(InventoryDashboardComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
