import { Component } from '@angular/core';
import { Router, RouterModule } from '@angular/router';

@Component({
  selector: 'app-count-spire-component',
  imports: [RouterModule],
  templateUrl: './count-spire-component.html',
  styleUrl: './count-spire-component.css',
})
export class CountSpireComponent {

  
constructor(private router: Router) {}

  navigate(action: string) {
    this.router.navigate(['/count', action]);
  }
}
