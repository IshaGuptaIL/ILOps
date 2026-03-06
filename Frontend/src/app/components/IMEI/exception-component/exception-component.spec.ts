import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ExceptionComponent } from './exception-component';

describe('ExceptionComponent', () => {
  let component: ExceptionComponent;
  let fixture: ComponentFixture<ExceptionComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ExceptionComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ExceptionComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
