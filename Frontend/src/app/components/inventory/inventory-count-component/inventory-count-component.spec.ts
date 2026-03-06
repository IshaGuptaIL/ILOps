import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InventoryCountComponent } from './inventory-count-component';

describe('InventoryCountComponent', () => {
  let component: InventoryCountComponent;
  let fixture: ComponentFixture<InventoryCountComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InventoryCountComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(InventoryCountComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
