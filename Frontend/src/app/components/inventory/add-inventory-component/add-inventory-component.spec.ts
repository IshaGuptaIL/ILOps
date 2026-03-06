import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AddInventoryComponent } from './add-inventory-component';

describe('AddInventoryComponent', () => {
  let component: AddInventoryComponent;
  let fixture: ComponentFixture<AddInventoryComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AddInventoryComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AddInventoryComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
