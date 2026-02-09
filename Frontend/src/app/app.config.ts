// src/main.ts or app.config.ts

import { ApplicationConfig, importProvidersFrom } from '@angular/core';
import { provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideAnimations } from '@angular/platform-browser/animations';
import { ToastrModule } from 'ngx-toastr';
import { routes } from './app.routes';
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';

// ✅ YE IMPORT MISSING THA
import { CookieService } from 'ngx-cookie-service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideClientHydration(withEventReplay()),
    provideAnimations(),

    CookieService, // ✅ ab sahi hai

    importProvidersFrom(
      ToastrModule.forRoot({
        progressBar: true,
        preventDuplicates: true,
        maxOpened: 1,
        autoDismiss: true,
        timeOut: 4000,
        positionClass: 'toast-top-right',
      })
    ),
  ],
};
