import { APP_INITIALIZER, ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { AppSettingsService } from './app-settings.service';
import { HTTP_INTERCEPTORS, provideHttpClient, withInterceptors } from '@angular/common/http';
import { AuthInterceptor, authInterceptor, provideAuth } from 'angular-auth-oidc-client';
import { authConfig } from './auth.config';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideAnimationsAsync(),
    provideHttpClient(withInterceptors([authInterceptor()])),
    provideAuth(authConfig),
    {
        provide: APP_INITIALIZER,
        useFactory: (appSettingsService: AppSettingsService) => () => appSettingsService.appSettings$,
        multi: true,
        deps: [AppSettingsService],
    },
],
};
