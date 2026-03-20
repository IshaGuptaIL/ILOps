import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CustomSearchComponent } from './custom-search-component';

describe('CustomSearchComponent', () => {
  let component: CustomSearchComponent;
  let fixture: ComponentFixture<CustomSearchComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CustomSearchComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CustomSearchComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
