import { Inject, ComponentFactoryResolver, ComponentFactory } from '@angular/core';
import { Http } from '@angular/http';
import { forEach } from '@angular/router/src/utils/collection';
import 'rxjs/add/operator/toPromise';
import { ChangesPropagator, IComponent } from '../components/changesPropagator/ChangesPropagator';
import { BaseViewModel, BaseComponent } from '../components/base.component';

export class Command {
    public constructor(
        public readonly commandId: number,
        public readonly viewModelTypeIds: number[]) {
    }
}
export class CommandInstruction {
    public readonly commandId: number;
    public readonly viewModelId: number;
    public readonly eventArgs: Exclude<any, null | undefined>;

    public constructor(
        commandId: number,
        viewModel: BaseViewModel | BaseComponent<any> | number,
        eventArgs?: any) {

        this.commandId = commandId;
        this.viewModelId = isNumeric(viewModel)
            ? <number>viewModel
            : (<BaseViewModel | BaseComponent<any>>viewModel).__id;
        this.eventArgs = eventArgs || 'null';
    }
}

export class CommandManager implements IComponent {
    [propertyName: string]: any;
    __id: number;

    public constructor(id: number) {
        this.__id = id;
    }
}

function isNumeric(n: any) {
    return !isNaN(parseFloat(n)) && isFinite(n);
}