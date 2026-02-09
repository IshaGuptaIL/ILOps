import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ImeiExceptionComponent } from './imei-exception-component';

describe('ImeiExceptionComponent', () => {
  let component: ImeiExceptionComponent;
  let fixture: ComponentFixture<ImeiExceptionComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ImeiExceptionComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ImeiExceptionComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
