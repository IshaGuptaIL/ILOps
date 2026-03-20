import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InventoryRunRateComponent } from './inventory-run-rate-component';

describe('InventoryRunRateComponent', () => {
  let component: InventoryRunRateComponent;
  let fixture: ComponentFixture<InventoryRunRateComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InventoryRunRateComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(InventoryRunRateComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
