import { Injectable } from '@angular/core';
import { environment } from '../../environments/environments.development';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs/internal/Observable';
import { ApiResponse } from '../components/IMEI/invoice-credit-service';

export interface MenuItem {
  id: number;
  label: string;
  icon: string;
  route?: string;
  children: MenuItem[];
  menuUrl?: string; // optional
}

@Injectable({
  providedIn: 'root',
})
export class RoleService {
 
  
  readonly ApiUrl = environment.apiUrl+"/User";

constructor(private http:HttpClient) {
  
}



 getRoles() {
    return this.http.get<any>(`${this.ApiUrl}/GetUserRoles`);
  }

  addRole(name: string) {
    return this.http.post(this.ApiUrl, { name });
  }

  updateRole(id: number, name: string) {
    return this.http.put(`${this.ApiUrl}/${id}`, { name });
  }

  toggleRole(id: number) {
    return this.http.patch(`${this.ApiUrl}/${id}/toggle`, {});
  }
 // Add user Below

    getUsers(page: number, pageSize: number) {
    return this.http.get<any>(
      `${this.ApiUrl}/GetUsers?page=${page}&pageSize=${pageSize}`
    );
  }

  getUserById(id: number) {
    return this.http.get<any>(
      `${this.ApiUrl}/GetUserById?id=${id}`
    );
  }

  createUser(data: any) {
    return this.http.post<any>(
      `${this.ApiUrl}/CreateUser`,
      data
    );
  }

  updateUser(data: any) {
    return this.http.post<any>(
      `${this.ApiUrl}/UpdateUser`,
      data
    );
  }

  deleteUser(id: number) {
    return this.http.delete<any>(
      `${this.ApiUrl}/DeleteUser?id=${id}`
    );
  }

// ===== Sidebar Menu APIs =====
  getUserMenus(roleId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.ApiUrl}/GetUserMenuPermissions?roleId=${roleId}`);
  }

  // ===== Helper to build tree structure from flat menus =====
//  buildTree(flatMenus: any[]): MenuItem[] {
//     const map = new Map<number, MenuItem>();
//     const roots: MenuItem[] = [];

//     // Initialize map with all menu items
//     flatMenus.forEach(m => {
//       map.set(m.id, {
//         id: m.id,
//         label: m.menuName,
//         icon: m.icon ?? 'bi-house-door',
//         route: m.controller ? '/' + m.controller : '#',
//         children: [],
//         menuUrl: m.menuUrl
//       });
//     });

//     // Assign children to parents
//  map.forEach(menu => {
//   const parentId = flatMenus.find(m => m.id === menu.id)?.parentId;

//   if (parentId && parentId !== 0) {
//     const parent = map.get(parentId);
//     if (parent) {
//       parent.children.push(menu); // ✅ safe now
//     }
//   } else {
//     roots.push(menu);
//   }
// });

//     return roots;
//   }
buildTree(flatMenus: any[]): MenuItem[] {
    const map = new Map<number, MenuItem>();
    const roots: MenuItem[] = [];

    // 1. Pehle pure data ko IndexId ke basis par sort kar lein (Double safety)
    flatMenus.sort((a, b) => (a.indexId || 0) - (b.indexId || 0));

    flatMenus.forEach(m => {
        map.set(m.id, {
            id: m.id,
            label: m.menuName,
            icon: m.icon ?? 'bi-house-door',
            route: m.controller ? '/' + m.controller : '#',
            children: [],
            menuUrl: m.menuUrl
        });
    });

    map.forEach(menu => {
        const parentId = flatMenus.find(m => m.id === menu.id)?.parentId;

        if (parentId && parentId !== 0) {
            const parent = map.get(parentId);
            if (parent) {
                parent.children.push(menu);
            }
        } else {
            roots.push(menu);
        }
    });

    return roots;
}



  // Role Service
   createRole(form: any) {
    return this.http.post<any>(
      this.ApiUrl + '/AddUserRole',
      { name: form.name }
    );
  }

  updateRoleForm(form: any) {
    return this.http.post<any>(
      this.ApiUrl + '/UpdateUserRole',
      {
        id: form.id,
        name: form.name
      }
    );
  }

  toggleActive(id: number) {
    return this.http.post<any>(
      this.ApiUrl + '/GetByIDUserRole?id=' + id,
      {}
    );
  }



  getActiveRoles(): Observable<ApiResponse> {
    return this.http.get<ApiResponse>(`${this.ApiUrl}/GetActiveRoles`);
  }

  getMenus(): Observable<ApiResponse> {
    return this.http.get<ApiResponse>(`${this.ApiUrl}/GetMenus`);
  }

  getRolePermissions(roleId: number): Observable<ApiResponse> {
    return this.http.get<ApiResponse>(`${this.ApiUrl}/GetRolePermissions?roleId=${roleId}`);
  }

saveRolePermissions(roleId: number, selectedMenus: number[]): Observable<any> {
  const payload = {
    roleId: roleId,       // Backend DTO key se match hona chahiye
    selectedMenus: selectedMenus
  };
  return this.http.post(`${this.ApiUrl}/SaveRolePermissions`, payload);
}


}
