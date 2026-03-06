import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class SpinnerService {
  // BehaviorSubject use karne se UI hamesha sync rahegi
  private _spinnerActive = new BehaviorSubject<boolean>(false);
  public readonly isActive$ = this._spinnerActive.asObservable();

  show(): void { this._spinnerActive.next(true); }
  hide(): void { this._spinnerActive.next(false); }
}