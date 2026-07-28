import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { Router, RouterLink } from '@angular/router';  // ✅ RouterLink IMPORT
import { CommonModule, NgFor, NgIf } from '@angular/common';
import { CookieService } from 'ngx-cookie-service';
import { MenuItem, RoleService } from '../../user-role-component/role-service';

interface MenuItemExtended extends MenuItem {
  expanded?: boolean;
}

@Component({
  selector: 'app-sidebar-component',
  imports: [CommonModule, NgFor, NgIf, RouterLink],  // ✅ RouterLink ADDED
  templateUrl: './sidebar-component.html',
  styleUrls: ['./sidebar-component.css'],
})
export class SidebarComponent implements OnInit {
  menuItems: MenuItemExtended[] = [];
  roleId: number | null = null;

  constructor(
    private cookieService: CookieService,
    private router: Router,
    private roleService: RoleService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const storedRole = Number(this.cookieService.get('UserRoleId'));
    this.roleId = storedRole || null;

    if (this.roleId) {
      this.loadMenus();
    }
  }

  toggleMenu(item: MenuItemExtended): void {
    item.expanded = !item.expanded;
    
    // Close other menus (optional)
    this.menuItems.forEach(menu => {
      if (menu !== item && menu.children && menu.children.length > 0) {
        menu.expanded = false;
      }
    });
     this.cdr.detectChanges(); 
  }

  
 private loadMenus(): void {
  this.roleService.getUserMenus(this.roleId!).subscribe({
    next: (res: any) => {
      if (Array.isArray(res.result) && res.result.length > 0) {
        this.menuItems = this.roleService
          .buildTree(res.result)
          .map((menu, index) => {
            const extendedMenu = this.ensureChildren(menu as MenuItemExtended);
            // ✅ Default open for Inventory menu (index 1)
            if (index === 1) {
              extendedMenu.expanded = true;
            }
            return extendedMenu;
          });

        console.log('✅ MenuItems loaded:', this.menuItems);

        const salesMenu = this.menuItems.find(m => m.label.toLowerCase().includes('sales'));
        if (salesMenu) {
          if (!salesMenu.children) salesMenu.children = [];
          if (!salesMenu.children.some(c => c.menuUrl === 'rogersInvoiceSpire')) {
            salesMenu.children.push({
              id: 9999,
              label: 'RogersInvoice-Spire',
              icon: 'bi-file-earmark-bar-graph',
              menuUrl: 'rogersInvoiceSpire',
              route: '/rogersInvoiceSpire',
              children: []
            });
          }
        } else {
          if (!this.menuItems.some(m => m.menuUrl === 'rogersInvoiceSpire')) {
            this.menuItems.push({
              id: 9999,
              label: 'RogersInvoice-Spire',
              icon: 'bi-file-earmark-bar-graph',
              menuUrl: 'rogersInvoiceSpire',
              route: '/rogersInvoiceSpire',
              children: []
            });
          }
        }
      }
      this.cdr.detectChanges();
    },
    error: (err) => {
      console.error('❌ Menu load failed:', err);
      this.cdr.detectChanges();
    }
  });
}

  logout(): void {
    this.cookieService.delete('token', '/');
    localStorage.removeItem('UserRoleId');
    this.router.navigate(['/login']);
  }

  private ensureChildren(menu: MenuItemExtended): MenuItemExtended {
    return {
      ...menu,
      children: (menu.children || []).map(child => this.ensureChildren(child as MenuItemExtended)),
      route: menu.route || '#',
      expanded: false
    };
  }
}
