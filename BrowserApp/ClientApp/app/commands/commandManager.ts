import 'rxjs/add/operator/toPromise';
import { BaseViewModel, ICommandManager, BaseComponent } from '../components/base.component';
import { CommandInstruction } from './commandInstruction';
import { Command, CommandBindingWithCommandName, ClientsideOptimizationCommand, OptimizationCanExecute } from './commands';
import { ConditionAST } from './ConditionAST'
import { CanonicalInputBinding, Kind } from './inputBindingParser';
import { InputEvent } from './inputTypes';
import { Component, Input } from '@angular/core';

@Component({
    selector: 'commandmanager',
    templateUrl: './commandManager.html',
    styleUrls: []
})
export class CommandManager extends BaseComponent<CommandManagerViewModel> implements ICommandManager {
    // @ts-ignore: server has no initializer

    public get flags() {
        return this.viewModel.flags;
    }
    public get commands() {
        return this.viewModel.commands;
    }
    public get inputBindings() {
        return this.viewModel.inputBindings;
    }
    public get id() {
        return this.viewModel.__id;
    }
    public constructor() {
        super();
    }

    public optimizeClientside(commandName: string, command: ClientsideOptimizationCommand) {
        if (commandName == null || commandName == '') {
            throw new Error('Invalid command name specified');
        }

        const binding = this.commands[commandName];
        if (binding === undefined) {
            throw new Error(`The command '${name}' is not registered at the command manager`);
        }

        binding.clientsideOptimization = command;
    }
    public bind(commandName: string, inputBinding: string, condition: string = "") {
        const commandBinding = this.commands[commandName];
        if (commandBinding === undefined) {
            throw new Error(`The command '${name}' is not registered at the command manager`);
        }

        const canonicalInput = CanonicalInputBinding.parse(inputBinding);
        const conditionAST = ConditionAST.parse(condition, this.flags);

        const hasExistingBindingForThisInput = this.inputBindings.has(canonicalInput);
        if (!hasExistingBindingForThisInput) {
            this.inputBindings.set(canonicalInput, []);
        }
        this.inputBindings.get(canonicalInput)!.push(new CommandBindingWithCommandName(commandName, conditionAST, canonicalInput));

    }
    public hasCommand(name: string): boolean {
        return this.commands[name] !== undefined;
    }


    /**
       * 
       * @param sender
       * @param commandName
       * @param e Optional event args, which is consumed if specified (i.e. propagation is stopped).
       */
    public executeCommandByName(commandName: string, sender: BaseViewModel, e?: InputEvent): void {
        const command = this.commands[commandName];
        if (command === undefined) {
            throw new Error(`The command '${commandName}' does not exist`);
        }
        if (!command.condition(this.flags).toBoolean(sender, e)) {
            console.warn(`The command '${commandName}' cannot execute on '${Object.getPrototypeOf(this).constructor.name}'(id=${sender.__id})`);
            return;
        }

        this.executeCommandIfPossible(command, sender, e);
    }
    public handleMouseMove(sender: BaseViewModel, e: MouseEvent): void {

        this.handle(CanonicalInputBinding.fromMouseMoveEvent(e), sender, e);
    }
    public handleMouseClick(sender: BaseViewModel, e: MouseEvent): void {
        // in javascript a mouse click is a left mouse button down and up event together, triggered at the moment of up event
        // for simplicity I simply trigger another kind of event, but I think the canonical input 'click' could be inferred from up and down events
        this.handle(CanonicalInputBinding.fromMouseEvent(e, Kind.Click), sender, e);
    }
    public handleMouseDown(sender: BaseViewModel, e: MouseEvent): void {
        this.handle(CanonicalInputBinding.fromMouseEvent(e, Kind.Down), sender, e);
    }
    public handleMouseUp(sender: BaseViewModel, e: MouseEvent): void {
        this.handle(CanonicalInputBinding.fromMouseEvent(e, Kind.Up), sender, e);
    }
    public handleKeypress(sender: BaseViewModel, e: KeyboardEvent): void {
        // key up events aren't handled (yet)
        this.handle(CanonicalInputBinding.fromKeyboardEvent(e), sender, e);
    }

    private handle(
        inputBinding: CanonicalInputBinding,
        sender: BaseViewModel,
        e: InputEvent): void {
        const commands = this.getExecutableCommandsBoundTo(inputBinding, sender, e);

        if (commands.length != 0) {
            // decide here whether to invoke all executable bound commands, or merely the first one, or dependent on properties of InputEvent 
            this.executeCommandIfPossible(commands[0], sender, e);
            e.stopPropagation();
        }
    }

    /**
     * Filters on 
     * - whether a condition is bound to the specified input, 
     * - whether the binding condition holds,
     * - whether the command can currently be executed. 
     * @param inputBinding
     * @param sender
     * @param e
     */
    private getExecutableCommandsBoundTo(inputBinding: CanonicalInputBinding, sender: BaseViewModel, e: InputEvent): Command[] {
        const commandBindings = this.inputBindings.get(inputBinding);
        if (commandBindings === undefined) {
            return [];
        }

        const commandNames: string[] = [];

        commandBindings.forEach((binding: CommandBindingWithCommandName) => {
            const command = this.commands[binding.commandName];
            if (binding.condition.toBoolean(sender, e)) {
                commandNames.push(binding.commandName);
            }
        });

        return commandNames
            .map(commandName => this.commands[commandName]!)
            .filter(command => command.condition(this.flags).toBoolean(sender, e));
    }

    private executeCommandIfPossible(command: Command, sender: BaseViewModel, e?: InputEvent): boolean {

        if (!command.condition(this.flags).toBoolean(sender, e)) {
            return false;
        }

        const args = command.getEventArgs(e);

        const executeCommandCondition = command.clientsideOptimization === undefined
            ? OptimizationCanExecute.ServersideOnly
            : command.clientsideOptimization.canExecute(sender, args);

        if (executeCommandCondition != OptimizationCanExecute.False && args !== undefined) {
            args.stopPropagation();
        }

        if (executeCommandCondition & OptimizationCanExecute.ServersideOnly) {
            const e_serverSide = command.getEventArgs(args);
            this.server.executeCommand(new CommandInstruction(command.id, sender, e_serverSide));
        }

        if (executeCommandCondition & OptimizationCanExecute.ClientsideOnly) {
            command.clientsideOptimization!.execute(sender, args);
        }

        return true;
    }

}

export interface CommandManagerViewModel extends BaseViewModel {
    flags: Map<string, boolean>;
    commands: CommandsMap;
    inputBindings: Map<CanonicalInputBinding, CommandBindingWithCommandName[]>;
}


export interface CommandsMap {
    [s: string]: Command;
}
