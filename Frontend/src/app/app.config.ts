import { ApplicationConfig } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { jwtInterceptor } from './common/jwt.interceptor';
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';
import { provideToastr } from 'ngx-toastr';
import { CookieService } from 'ngx-cookie-service';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes, withComponentInputBinding()),
    provideClientHydration(withEventReplay()),
    provideAnimations(),
    provideHttpClient(withFetch(), withInterceptors([jwtInterceptor])),
    CookieService,
    provideToastr({
      progressBar: true,
      preventDuplicates: true,
      maxOpened: 1,
      autoDismiss: true,
      timeOut: 4000,
      positionClass: 'toast-top-right',
    }),
  ],
};
