import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class SpinnerService {
  private _spinnerActive = false;

  get isActive(): boolean {
    return this._spinnerActive;
  }

  show(): void {
    this._spinnerActive = true;
  }

  hide(): void {
    this._spinnerActive = false;
  }
}
