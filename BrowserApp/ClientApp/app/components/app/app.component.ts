import { Component, Inject, ComponentFactoryResolver, ComponentFactory, OnInit, PACKAGE_ROOT_URL, AfterViewInit } from '@angular/core';
import { Http } from '@angular/http';
import { ChangesPropagator, IComponent } from '../changesPropagator/ChangesPropagator';
import { CommandManager } from '../../commands/commands';
import { Counter } from '../counter/counter.component';
import { BaseComponent, BaseViewModel } from '../base.component';

@Component({
    selector: 'app',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.css']
})
export class AppComponent extends BaseComponent<App> {
    public get counter() { return this.viewModel.counter; }

    constructor(http: Http,
        componentFactoryResolver: ComponentFactoryResolver,
        @Inject('BASE_URL') baseUrl: string) {
        super(new ChangesPropagator(http, componentFactoryResolver, baseUrl, () => this.initialComponents()))

        this.server.open();
    }

    private initialComponents(): Iterable<BaseViewModel> {
        const viewModel: App = {
            __id: -1,
            commandManager: new CommandManager(0),
            counter: { currentCount: 0, __id: 1 },
        };
        (<any>this).viewModel = viewModel;

        const keys = <Exclude<keyof App, "__id">[]>Object.keys(viewModel).filter(key => key != "__id");
        return keys.map(key => viewModel[key]);
    }
}
export interface App extends BaseViewModel {
    counter: Counter;
    commandManager: CommandManager;
}

