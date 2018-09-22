import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { HttpModule} from '@angular/http';
import { RouterModule } from '@angular/router';
import { AppComponent } from './app/app.component';
import { CounterComponent } from './counter/counter.component';
import { CommandManager } from './view.index';

@NgModule({
  declarations: [
    AppComponent,
    CounterComponent,
    CommandManager
  ],
  imports: [
    BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
    HttpModule, // maybe upgrade view package to higher angular to remove this warning?
    HttpClientModule,
    FormsModule,
    RouterModule.forRoot([
      { path: '', component: AppComponent, pathMatch: 'full' },
      { path: 'counter', component: CounterComponent },
    ])
  ],
  providers: [
    { provide: 'BASE_URL', useFactory: getBaseUrl }
  ],
  bootstrap: [AppComponent]
})

export class AppModule { }

export function getBaseUrl() {
  return document.getElementsByTagName('base')[0].href;
}

