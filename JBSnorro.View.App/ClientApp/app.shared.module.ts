import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpModule, Http } from '@angular/http';
import { RouterModule } from '@angular/router';
import { CommandManager } from '.';
import { CounterComponent } from './components/counter/counter.component';
import { AppComponent } from './components/app/app.component';


@NgModule({
    declarations: [
        AppComponent,
        CounterComponent,
        CommandManager,
    ],
    imports: [
        CommonModule,
        HttpModule,
        FormsModule,
        RouterModule.forRoot([
            { path: '', redirectTo: 'home', pathMatch: 'full' },
            { path: 'home', component: CounterComponent },
            { path: '**', redirectTo: 'home' }
        ])
    ],
})
export class AppModuleShared {
}
