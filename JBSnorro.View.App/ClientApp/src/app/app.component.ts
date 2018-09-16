import { Component, Inject } from '@angular/core';
import { AppBaseComponent, AppBase } from './../view.index';
import { Http } from '@angular/http';
import { Counter } from './counter/counter.component';

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.css']
})
export class AppComponent extends AppBaseComponent<App> {
    constructor(http: Http, @Inject('BASE_URL') baseUrl: string) {
        super(<any>http, baseUrl);

        this.server.open();
    }
    title = 'app';
    protected populateInitialViewModel(appBaseViewModel: App): void {
    }
    public get counter(): Counter {
        return this.viewModel.counter;
    }
}
export interface App extends AppBase {
    counter: Counter;
}
