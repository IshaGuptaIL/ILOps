import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from './components/sidebar-component/sidebar-component';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { CommonModule } from '@angular/common';
import { Spinner } from './components/shared/spinner/spinner';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet,SidebarComponent,CommonModule,Spinner],
  templateUrl: './app.html',
styleUrls: ['./app.css'] 
})
export class App {
  protected readonly title = signal('LegacyApp');
  showSidebar = false;  

  constructor(private router: Router) {
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe((event: NavigationEnd) => {
      const url = event.urlAfterRedirects;
      this.showSidebar = !(url === '/' || url === '/login');
    });
  }
}
